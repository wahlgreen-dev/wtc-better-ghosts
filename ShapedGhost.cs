using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSpeed.GhostCar;
using UnityEngine;

namespace BetterGhosts;

public class ShapedGhost
{
    private const string GenericModelName = "GhostCarModel";
    private const string GenericBodyName = "Body";

    private readonly GameObject root;
    private readonly GhostPalette palette;
    private readonly GameObject genericModel;
    private readonly Transform[] partPivots;
    private readonly PartRecording parts;
    private readonly AnimatedRig rig;
    private readonly LegAnimation legAnimation;
    private readonly List<float> bodyTimes = new();
    private readonly List<Vector3> bodyPositions = new();
    private readonly List<Quaternion> bodyRotations = new();
    private readonly List<Vector3> bodyVelocities = new();
    private readonly List<bool> bodyGrounded = new();
    private readonly List<float> bodySkids = new();
    private readonly SampleCursor bodyCursor;
    private readonly List<Mesh> bakedMeshes = new();

    public ShapedGhost(GhostCarManager manager, Transform vehicle, Transform body, Transform[] parts, VehicleSetup setup, PartRecording recording)
    {
        Transform modelTransform = TransformPaths.FindDescendant(manager.ghostTransform, GenericModelName);
        Renderer ghostRenderer = TransformPaths.FindDescendant(modelTransform, GenericBodyName).GetComponent<Renderer>();
        palette = new GhostPalette(ghostRenderer);
        int layer = ghostRenderer.gameObject.layer;

        CacheSamples(manager.existingGhostData);
        bodyCursor = new SampleCursor(bodyTimes);
        if (recording != null && recording.SampleCount == bodyTimes.Count)
            this.parts = recording;

        root = new GameObject("BetterGhost");
        root.layer = layer;
        root.transform.SetParent(modelTransform.parent, false);
        root.transform.localScale = body.lossyScale;

        partPivots = new Transform[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            partPivots[i] = CreatePivot(parts[i], body, layer);

        Transform animated = setup.LegAnimator != null ? setup.LegAnimator.transform : null;
        CopyRenderers(vehicle, body, parts, animated, layer);

        if (setup.LegAnimator != null)
        {
            rig = new AnimatedRig(setup.LegAnimator, body, root.transform, palette);
            legAnimation = new LegAnimation(vehicle, setup.LegAnimator);
        }

        genericModel = modelTransform.gameObject;
        genericModel.SetActive(false);
    }

    public void Update(float time, float deltaTime)
    {
        bodyCursor.Seek(time, out int from, out int to, out float blend);
        root.transform.position = Vector3.Lerp(bodyPositions[from], bodyPositions[to], blend);
        root.transform.rotation = Quaternion.Slerp(bodyRotations[from], bodyRotations[to], blend);

        if (parts != null)
            UpdateParts(from, to, blend);

        if (rig != null)
            UpdateRig(from, to, blend, deltaTime);
    }

    public void Destroy()
    {
        if (genericModel != null)
            genericModel.SetActive(true);
        if (root != null)
            Object.Destroy(root);

        rig?.Destroy();
        palette.Destroy();
        foreach (Mesh mesh in bakedMeshes)
            Object.Destroy(mesh);
    }

    private void UpdateParts(int from, int to, float blend)
    {
        for (int i = 0; i < partPivots.Length; i++)
        {
            partPivots[i].localPosition = Vector3.Lerp(parts.Positions[from][i], parts.Positions[to][i], blend);
            partPivots[i].localRotation = Quaternion.Slerp(parts.Rotations[from][i], parts.Rotations[to][i], blend);
        }
    }

    private void UpdateRig(int from, int to, float blend, float deltaTime)
    {
        GhostMotion motion = new GhostMotion
        {
            Position = root.transform.position,
            Rotation = root.transform.rotation,
            Velocity = Vector3.Lerp(bodyVelocities[from], bodyVelocities[to], blend),
            IsGrounded = blend < 0.5f ? bodyGrounded[from] : bodyGrounded[to],
            Skid = Mathf.Lerp(bodySkids[from], bodySkids[to], blend),
        };
        legAnimation.Drive(rig.Animator, motion, deltaTime);
        rig.BakeSkins();
    }

    private void CacheSamples(GhostCarData data)
    {
        var samples = data.ghostData;
        for (int i = 0; i < samples.Count; i++)
        {
            GhostCarInstantSaveData sample = samples[i];
            bodyTimes.Add(sample.time);
            bodyPositions.Add(sample.position);
            bodyRotations.Add(sample.rotation);
            bodyVelocities.Add(sample.velocity);
            bodyGrounded.Add(sample.isGrounded);
            bodySkids.Add(sample.skid);
        }
    }

    private Transform CreatePivot(Transform part, Transform body, int layer)
    {
        Transform pivot = new GameObject(part.name).transform;
        pivot.gameObject.layer = layer;
        pivot.SetParent(root.transform, false);
        RelativePose.Copy(pivot, part, body);
        return pivot;
    }

    private void CopyRenderers(Transform vehicle, Transform body, Transform[] parts, Transform animated, int layer)
    {
        Il2CppArrayBase<Renderer> renderers = vehicle.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer.enabled || TransformPaths.HasComponentNamed(renderer.transform, "Billboard"))
                continue;
            if (animated != null && renderer.transform.IsChildOf(animated))
                continue;

            int anchorIndex = FindAnchor(renderer.transform, body, parts);
            Transform anchor = anchorIndex >= 0 ? parts[anchorIndex] : body;
            Transform parent = anchorIndex >= 0 ? partPivots[anchorIndex] : root.transform;

            SkinnedMeshRenderer skinned = renderer.TryCast<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                CopyRenderer(renderer, Bake(skinned), null, anchor, parent, layer);
                continue;
            }

            if (renderer.TryCast<MeshRenderer>() == null)
                continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            Mesh mesh = filter.sharedMesh;
            CopyRenderer(renderer, mesh, palette.SlotStream(mesh.vertexCount), anchor, parent, layer);
        }
    }

    private Mesh Bake(SkinnedMeshRenderer skinned)
    {
        Mesh baked = new Mesh();
        skinned.BakeMesh(baked);
        baked.SetUVs(GhostPalette.SlotChannel, GhostPalette.Slots(baked.vertexCount));
        bakedMeshes.Add(baked);
        return baked;
    }

    private static int FindAnchor(Transform transform, Transform body, Transform[] parts)
    {
        for (Transform t = transform; t != null && t != body; t = t.parent)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == t)
                    return i;
            }
        }
        return -1;
    }

    private void CopyRenderer(Renderer source, Mesh mesh, Mesh paletteSlotStream, Transform anchor, Transform parent, int layer)
    {
        GameObject copy = new GameObject(source.gameObject.name);
        copy.layer = layer;
        copy.transform.SetParent(parent, false);
        RelativePose.Copy(copy.transform, source.transform, anchor);

        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = copy.AddComponent<MeshRenderer>();
        if (paletteSlotStream != null)
            renderer.additionalVertexStreams = paletteSlotStream;
        palette.Paint(renderer, mesh.subMeshCount);
    }
}
