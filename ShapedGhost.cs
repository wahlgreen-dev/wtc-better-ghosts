using System.Collections.Generic;
using Il2CppSpeed.GhostCar;
using UnityEngine;

namespace BetterGhosts;

public class ShapedGhost
{
    const string GenericModelName = "GhostCarModel";

    readonly GameObject root;
    readonly GhostPalette palette;
    readonly GameObject genericModel;
    readonly Transform[] partPivots;
    readonly PartRecording parts;
    readonly AnimatedRig rig;
    readonly LegAnimation legAnimation;
    readonly List<float> bodyTimes = new();
    readonly List<Vector3> bodyPositions = new();
    readonly List<Quaternion> bodyRotations = new();
    readonly List<Vector3> bodyVelocities = new();
    readonly List<bool> bodyGrounded = new();
    readonly List<float> bodySkids = new();
    readonly SampleCursor bodyCursor;
    readonly List<Mesh> bakedMeshes = new();

    public ShapedGhost(GhostCarManager manager, Transform vehicle, Transform body, Transform[] parts,
        VehicleSetup setup, PartRecording recording)
    {
        var ghost = manager.ghostTransform;
        var modelTransform = TransformPaths.FindDescendant(ghost, GenericModelName);
        var ghostRenderer = FindGhostRenderer(modelTransform != null ? modelTransform : ghost);
        if (ghostRenderer == null)
            throw new System.InvalidOperationException("the game's ghost has no mesh renderer to take the material from");
        palette = new GhostPalette(ghostRenderer);

        CacheBodySamples(manager.existingGhostData);
        bodyCursor = new SampleCursor(bodyTimes);
        if (recording != null && recording.SampleCount == bodyTimes.Count)
            this.parts = recording;

        root = new GameObject("BetterGhost");
        root.layer = ghostRenderer.gameObject.layer;
        root.transform.SetParent(modelTransform != null ? modelTransform.parent : ghost, false);
        root.transform.localScale = body.lossyScale;

        partPivots = new Transform[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            partPivots[i] = CreatePivot(parts[i], body, root.transform, ghostRenderer.gameObject.layer);

        var animated = setup.LegAnimator != null ? setup.LegAnimator.transform : null;
        CopyRenderers(vehicle, body, parts, animated, ghostRenderer.gameObject.layer);

        if (setup.LegAnimator != null)
        {
            rig = new AnimatedRig(setup.LegAnimator, body, root.transform, palette);
            legAnimation = new LegAnimation(vehicle, setup.LegAnimator);
        }

        if (modelTransform != null)
        {
            genericModel = modelTransform.gameObject;
            genericModel.SetActive(false);
        }
    }

    public void Update(float time, float deltaTime)
    {
        if (root == null)
            return;

        if (!bodyCursor.Seek(time, out var from, out var to, out var blend))
            return;

        root.transform.position = Vector3.Lerp(bodyPositions[from], bodyPositions[to], blend);
        root.transform.rotation = Quaternion.Slerp(bodyRotations[from], bodyRotations[to], blend);

        if (parts != null)
        {
            for (int i = 0; i < partPivots.Length; i++)
            {
                partPivots[i].localPosition = Vector3.Lerp(parts.Positions[from][i], parts.Positions[to][i], blend);
                partPivots[i].localRotation = Quaternion.Slerp(parts.Rotations[from][i], parts.Rotations[to][i], blend);
            }
        }

        if (rig != null)
        {
            var motion = new GhostMotion
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
    }

    public void Destroy()
    {
        if (genericModel != null)
            genericModel.SetActive(true);
        if (root != null)
            Object.Destroy(root);
        rig?.Destroy();
        palette.Destroy();
        foreach (var mesh in bakedMeshes)
        {
            if (mesh != null)
                Object.Destroy(mesh);
        }
    }

    void CacheBodySamples(GhostCarData data)
    {
        if (data == null || data.ghostData == null)
            return;

        var samples = data.ghostData;
        for (int i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            bodyTimes.Add(sample.time);
            bodyPositions.Add(sample.position);
            bodyRotations.Add(sample.rotation);
            bodyVelocities.Add(sample.velocity);
            bodyGrounded.Add(sample.isGrounded);
            bodySkids.Add(sample.skid);
        }
    }

    static Transform CreatePivot(Transform part, Transform body, Transform parent, int layer)
    {
        var pivot = new GameObject(part.name).transform;
        pivot.gameObject.layer = layer;
        pivot.SetParent(parent, false);
        pivot.localPosition = body.InverseTransformPoint(part.position);
        pivot.localRotation = Quaternion.Inverse(body.rotation) * part.rotation;
        pivot.localScale = Divide(part.lossyScale, body.lossyScale);
        return pivot;
    }

    void CopyRenderers(Transform vehicle, Transform body, Transform[] parts, Transform animated, int layer)
    {
        var renderers = vehicle.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (!renderer.enabled || TransformPaths.HasComponentNamed(renderer.transform, "Billboard"))
                continue;
            if (animated != null && renderer.transform.IsChildOf(animated))
                continue;

            var anchorIndex = FindAnchor(renderer.transform, body, parts);
            var anchor = anchorIndex >= 0 ? parts[anchorIndex] : body;
            var parent = anchorIndex >= 0 ? partPivots[anchorIndex] : root.transform;

            var skinned = renderer.TryCast<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                CopyRenderer(renderer, BakeWithGhostPalette(skinned), null, anchor, parent, layer);
                continue;
            }

            if (renderer.TryCast<MeshRenderer>() == null)
                continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            var mesh = filter.sharedMesh;
            CopyRenderer(renderer, mesh, palette.SlotStream(mesh.vertexCount), anchor, parent, layer);
        }
    }

    Mesh BakeWithGhostPalette(SkinnedMeshRenderer skinned)
    {
        var baked = new Mesh();
        skinned.BakeMesh(baked);
        baked.SetUVs(GhostPalette.SlotChannel, GhostPalette.Slots(baked.vertexCount));
        bakedMeshes.Add(baked);
        return baked;
    }

    static int FindAnchor(Transform transform, Transform body, Transform[] parts)
    {
        for (var t = transform; t != null && t != body; t = t.parent)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == t)
                    return i;
            }
        }
        return -1;
    }

    void CopyRenderer(Renderer source, Mesh mesh, Mesh paletteSlotStream, Transform anchor, Transform parent, int layer)
    {
        var copy = new GameObject(source.gameObject.name);
        copy.layer = layer;
        var transform = copy.transform;
        transform.SetParent(parent, false);
        transform.localPosition = anchor.InverseTransformPoint(source.transform.position);
        transform.localRotation = Quaternion.Inverse(anchor.rotation) * source.transform.rotation;
        transform.localScale = Divide(source.transform.lossyScale, anchor.lossyScale);

        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = copy.AddComponent<MeshRenderer>();
        if (paletteSlotStream != null)
            renderer.additionalVertexStreams = paletteSlotStream;
        palette.Paint(renderer, mesh.subMeshCount);
    }

    static Renderer FindGhostRenderer(Transform model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        Renderer fallback = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer.TryCast<MeshRenderer>() == null && renderer.TryCast<SkinnedMeshRenderer>() == null)
                continue;
            if (renderer.gameObject.name == "Body")
                return renderer;
            fallback ??= renderer;
        }
        return fallback;
    }

    static Vector3 Divide(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
}
