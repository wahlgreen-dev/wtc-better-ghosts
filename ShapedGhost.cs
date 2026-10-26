using System.Collections.Generic;
using Il2Cpp;
using Il2CppSpeed.GhostCar;
using UnityEngine;
using UnityEngine.Rendering;

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
    private readonly CopiedRig rig;
    private readonly GameObject physicsContainer;
    private readonly PowerEffects power = new();
    private readonly LegAnimation legAnimation;
    private readonly UmbrellaAnimation umbrella;
    private readonly List<float> bodyTimes = new();
    private readonly List<Vector3> bodyPositions = new();
    private readonly List<Quaternion> bodyRotations = new();
    private readonly List<Vector3> bodyVelocities = new();
    private readonly List<bool> bodyGrounded = new();
    private readonly List<float> bodySkids = new();
    private readonly List<bool> actionPressed = new();
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

        root = new("BetterGhost");
        root.layer = layer;
        root.transform.SetParent(modelTransform.parent, false);
        root.transform.localScale = body.lossyScale;

        partPivots = new Transform[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            partPivots[i] = CreatePivot(parts[i], body, layer);

        Transform rigRoot = setup.RigRoot;
        CopyRenderers(vehicle, body, parts, rigRoot, layer);
        CopyPowerEffects(vehicle, body, parts, rigRoot);

        if (setup.PhysicsRig != null)
        {
            physicsContainer = new("BetterGhostPhysics");
            physicsContainer.layer = layer;
            root.transform.SetPositionAndRotation(bodyPositions[0], bodyRotations[0]);
        }

        if (rigRoot != null)
            rig = new(rigRoot, body, root.transform, palette, power, physicsContainer != null ? physicsContainer.transform : null);
        if (setup.LegAnimator != null)
            legAnimation = new(vehicle, setup.LegAnimator);
        if (rigRoot != null)
            umbrella = UmbrellaAnimation.Find(vehicle, rigRoot, rig);

        genericModel = modelTransform.gameObject;
        genericModel.SetActive(false);
    }

    public void Update(float time, float deltaTime)
    {
        bodyCursor.Seek(time, out int from, out int to, out float blend);
        root.transform.position = Vector3.Lerp(bodyPositions[from], bodyPositions[to], blend);
        root.transform.rotation = Quaternion.Slerp(bodyRotations[from], bodyRotations[to], blend);

        if (physicsContainer != null)
            MatchPhysicsVisibility();

        if (parts != null)
            UpdateParts(from, to, blend);

        bool pressed = blend < 0.5f ? actionPressed[from] : actionPressed[to];
        umbrella?.Update(pressed, deltaTime);

        if (rig != null)
            UpdateRig(from, to, blend, deltaTime);

        power.SetActive(pressed);
    }

    public void Destroy()
    {
        if (genericModel != null)
            genericModel.SetActive(true);
        if (root != null)
            Object.Destroy(root);
        if (physicsContainer != null)
            Object.Destroy(physicsContainer);

        rig?.Destroy();
        palette.Destroy();
        foreach (Mesh mesh in bakedMeshes)
            Object.Destroy(mesh);
    }

    private void MatchPhysicsVisibility()
    {
        bool visible = root.activeInHierarchy;
        if (physicsContainer.activeSelf != visible)
            physicsContainer.SetActive(visible);
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
        if (legAnimation != null)
            DriveLegs(from, to, blend, deltaTime);
        rig.BakeSkins();
    }

    private void DriveLegs(int from, int to, float blend, float deltaTime)
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
            actionPressed.Add(sample.actionPressed);
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

    private void CopyRenderers(Transform vehicle, Transform body, Transform[] parts, Transform rigRoot, int layer)
    {
        var renderers = vehicle.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer.enabled || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly || renderer.GetComponent<Billboard>() != null)
                continue;
            if (rigRoot != null && renderer.transform.IsChildOf(rigRoot))
                continue;
            if (renderer.GetComponentInParent<Fire>(true) != null)
                continue;

            FindPlacement(renderer.transform, body, parts, out Transform anchor, out Transform parent);

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

    private void CopyPowerEffects(Transform vehicle, Transform body, Transform[] parts, Transform rigRoot)
    {
        var fires = vehicle.GetComponentsInChildren<Fire>(true);
        for (int i = 0; i < fires.Length; i++)
        {
            if (!fires[i].gameObject.activeInHierarchy)
                continue;
            if (rigRoot != null && fires[i].transform.IsChildOf(rigRoot))
                continue;

            FindPlacement(fires[i].transform, body, parts, out Transform anchor, out Transform parent);
            power.AddDetachedFire(fires[i], anchor, parent);
        }

        var blocks = vehicle.GetComponentsInChildren<JetpackBuildingBlock>(true);
        for (int i = 0; i < blocks.Length; i++)
        {
            if (!blocks[i].gameObject.activeInHierarchy)
                continue;
            if (rigRoot != null && blocks[i].transform.IsChildOf(rigRoot))
                continue;

            FindPlacement(blocks[i].transform, body, parts, out Transform anchor, out Transform parent);
            power.AddDetachedBlock(blocks[i], anchor, parent);
        }
    }

    private void FindPlacement(Transform source, Transform body, Transform[] parts, out Transform anchor, out Transform parent)
    {
        int anchorIndex = FindAnchor(source, body, parts);
        anchor = anchorIndex >= 0 ? parts[anchorIndex] : body;
        parent = anchorIndex >= 0 ? partPivots[anchorIndex] : root.transform;
    }

    private Mesh Bake(SkinnedMeshRenderer skinned)
    {
        Mesh baked = new Mesh();
        skinned.BakeMesh(baked);
        baked.SetUVs(GhostPalette.SlotChannel, palette.Slots(baked.vertexCount));
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
