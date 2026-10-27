using System.Collections.Generic;
using System.Linq;
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
    private readonly List<Transform> anchorSources = new();
    private readonly List<Transform> anchorPivots = new();
    private readonly List<SpinningPart> spinners = new();
    private readonly PartRecording recording;
    private readonly List<InferredWheel> inferredWheels = new();
    private readonly CopiedRig rig;
    private readonly GameObject physicsContainer;
    private readonly PowerEffects power = new();
    private AnimatorDriver animatorDriver;
    private Animator drivenAnimator;
    private readonly UmbrellaAnimation umbrella;
    private readonly RiderPose riderPose;
    private readonly List<float> bodyTimes = new();
    private readonly List<Vector3> bodyPositions = new();
    private readonly List<Quaternion> bodyRotations = new();
    private readonly List<Vector3> bodyVelocities = new();
    private readonly List<bool> bodyGrounded = new();
    private readonly List<float> bodySkids = new();
    private readonly List<bool> actionPressed = new();
    private readonly SampleCursor bodyCursor;
    private readonly List<Mesh> bakedMeshes = new();
    private readonly List<GoalObjectGhost> goalObjectGhosts = new();

    public ShapedGhost(GhostCarManager manager, Transform vehicle, Transform body, Transform[] parts, Transform[] goalObjects, VehicleSetup setup, PartRecording recording)
    {
        Transform modelTransform = TransformPaths.FindDescendant(manager.ghostTransform, GenericModelName);
        Renderer ghostRenderer = TransformPaths.FindDescendant(modelTransform, GenericBodyName).GetComponent<Renderer>();
        palette = new GhostPalette(ghostRenderer);
        int layer = ghostRenderer.gameObject.layer;

        CacheSamples(manager.existingGhostData);
        bodyCursor = new SampleCursor(bodyTimes);
        if (recording != null && recording.SampleCount == bodyTimes.Count)
            this.recording = recording;

        root = new("BetterGhost");
        root.layer = layer;
        root.transform.SetParent(modelTransform.parent, false);
        root.transform.localScale = body.lossyScale;

        partPivots = new Transform[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            partPivots[i] = CreatePivot(parts[i], body, root.transform, layer);
            anchorSources.Add(parts[i]);
            anchorPivots.Add(partPivots[i]);
        }

        Transform rider = setup.RiderPath != null ? TransformPaths.Find(vehicle, setup.RiderPath) : null;
        Transform rigRoot = setup.RigRoot != null ? setup.RigRoot : rider;
        Transform rigParent = root.transform;
        if (rider != null)
        {
            rigParent = CreatePivot(rider, body, root.transform, layer);
            riderPose = new RiderPose(rigParent, rider, body);
        }

        if (setup.SpinWheels)
            CreateInferredWheels(vehicle, body, rigRoot, layer);

        CreateSpinnerPivots(vehicle, body, rigRoot, layer);
        CopyRenderers(vehicle, body, rigRoot, layer);
        CopyPowerEffects(vehicle, body, rigRoot);

        if (setup.PhysicsRig != null)
        {
            physicsContainer = new("BetterGhostPhysics");
            physicsContainer.layer = layer;
            root.transform.SetPositionAndRotation(bodyPositions[0], bodyRotations[0]);
        }

        if (rigRoot != null)
            rig = new(rigRoot, rider != null ? rider : body, rigParent, palette, power, physicsContainer != null ? physicsContainer.transform : null);
        if (rigRoot != null)
        {
            SetUpAnimator(vehicle, rigRoot, setup.LegAnimator);
            umbrella = UmbrellaAnimation.Find(vehicle, rigRoot, rig);
            FindRigSpinners(vehicle, rigRoot);
        }

        if (this.recording != null && this.recording.GoalObjectNames.SequenceEqual(GoalObjects.Names(goalObjects)))
        {
            foreach (Transform goalObject in goalObjects)
                goalObjectGhosts.Add(new GoalObjectGhost(goalObject, palette, layer));
        }

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

        if (recording != null)
            UpdateParts(from, to, blend);
        UpdateGoalObjects(from, to, blend);

        Vector3 velocity = Vector3.Lerp(bodyVelocities[from], bodyVelocities[to], blend);
        float speed = velocity.magnitude;
        riderPose?.Update(root.transform.position, velocity, deltaTime);
        foreach (SpinningPart spinner in spinners)
            spinner.Update(deltaTime, speed);
        foreach (InferredWheel wheel in inferredWheels)
            wheel.Update(time, deltaTime, speed);

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

        foreach (GoalObjectGhost goalObjectGhost in goalObjectGhosts)
            goalObjectGhost.Destroy();

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
            partPivots[i].localPosition = Vector3.Lerp(recording.Positions[from][i], recording.Positions[to][i], blend);
            partPivots[i].localRotation = Quaternion.Slerp(recording.Rotations[from][i], recording.Rotations[to][i], blend);
        }
    }

    private void UpdateGoalObjects(int from, int to, float blend)
    {
        bool visible = root.activeInHierarchy;
        for (int i = 0; i < goalObjectGhosts.Count; i++)
            goalObjectGhosts[i].Update(Vector3.Lerp(recording.GoalPositions[from][i], recording.GoalPositions[to][i], blend), visible);
    }

    private void CreateInferredWheels(Transform vehicle, Transform body, Transform rigRoot, int layer)
    {
        foreach (Transform wheel in InputWheels.Find(vehicle, rigRoot))
        {
            Transform pivot = CreatePivot(wheel, body, root.transform, layer);
            anchorSources.Add(wheel);
            anchorPivots.Add(pivot);
            inferredWheels.Add(new InferredWheel(pivot, wheel, body));
        }
    }

    private void UpdateRig(int from, int to, float blend, float deltaTime)
    {
        if (animatorDriver != null)
            UpdateAnimator(from, to, blend, deltaTime);
        rig.BakeSkins();
    }

    private void UpdateAnimator(int from, int to, float blend, float deltaTime)
    {
        GhostMotion motion = new GhostMotion
        {
            Position = root.transform.position,
            Rotation = root.transform.rotation,
            Velocity = Vector3.Lerp(bodyVelocities[from], bodyVelocities[to], blend),
            IsGrounded = blend < 0.5f ? bodyGrounded[from] : bodyGrounded[to],
            Skid = Mathf.Lerp(bodySkids[from], bodySkids[to], blend),
        };
        animatorDriver.Drive(drivenAnimator, motion, deltaTime);
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

    private static Transform CreatePivot(Transform source, Transform anchor, Transform parent, int layer)
    {
        Transform pivot = new GameObject(source.name).transform;
        pivot.gameObject.layer = layer;
        pivot.SetParent(parent, false);
        RelativePose.Copy(pivot, source, anchor);
        return pivot;
    }

    private void CreateSpinnerPivots(Transform vehicle, Transform body, Transform rigRoot, int layer)
    {
        var rotators = vehicle.GetComponentsInChildren<RotateAroundAxis>(true);
        for (int i = 0; i < rotators.Length; i++)
        {
            Transform source = rotators[i].transform;
            if (!SpinningPart.Spins(rotators[i]) || (rigRoot != null && source.IsChildOf(rigRoot)))
                continue;

            FindPlacement(source, body, out Transform anchor, out Transform parent);
            Transform pivot = CreatePivot(source, anchor, parent, layer);
            anchorSources.Add(source);
            anchorPivots.Add(pivot);
            spinners.Add(new SpinningPart(pivot, rotators[i]));
        }
    }

    private void FindRigSpinners(Transform vehicle, Transform rigRoot)
    {
        var rotators = vehicle.GetComponentsInChildren<RotateAroundAxis>(true);
        for (int i = 0; i < rotators.Length; i++)
        {
            Transform source = rotators[i].transform;
            if (!SpinningPart.Spins(rotators[i]) || !source.IsChildOf(rigRoot))
                continue;

            Transform copy = rig.CopyOf(source);
            if (copy != null)
                spinners.Add(new SpinningPart(copy, rotators[i]));
        }
    }

    private void SetUpAnimator(Transform vehicle, Transform rigRoot, Animator legAnimator)
    {
        Animator source = legAnimator != null ? legAnimator : rigRoot.GetComponentInChildren<Animator>(true);
        if (source == null)
            return;

        Transform copy = rig.CopyOf(source.transform);
        if (copy == null)
            return;

        drivenAnimator = copy.GetComponent<Animator>();
        animatorDriver = new AnimatorDriver(vehicle, source);
    }

    private void CopyRenderers(Transform vehicle, Transform body, Transform rigRoot, int layer)
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

            FindPlacement(renderer.transform, body, out Transform anchor, out Transform parent);

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

    private void CopyPowerEffects(Transform vehicle, Transform body, Transform rigRoot)
    {
        var fires = vehicle.GetComponentsInChildren<Fire>(true);
        for (int i = 0; i < fires.Length; i++)
        {
            if (!fires[i].gameObject.activeInHierarchy)
                continue;
            if (rigRoot != null && fires[i].transform.IsChildOf(rigRoot))
                continue;

            FindPlacement(fires[i].transform, body, out Transform anchor, out Transform parent);
            power.AddDetachedFire(fires[i], anchor, parent);
        }

        var blocks = vehicle.GetComponentsInChildren<JetpackBuildingBlock>(true);
        for (int i = 0; i < blocks.Length; i++)
        {
            if (!blocks[i].gameObject.activeInHierarchy)
                continue;
            if (rigRoot != null && blocks[i].transform.IsChildOf(rigRoot))
                continue;

            FindPlacement(blocks[i].transform, body, out Transform anchor, out Transform parent);
            power.AddDetachedBlock(blocks[i], anchor, parent);
        }
    }

    private void FindPlacement(Transform source, Transform body, out Transform anchor, out Transform parent)
    {
        int anchorIndex = FindAnchor(source, body);
        anchor = anchorIndex >= 0 ? anchorSources[anchorIndex] : body;
        parent = anchorIndex >= 0 ? anchorPivots[anchorIndex] : root.transform;
    }

    private Mesh Bake(SkinnedMeshRenderer skinned)
    {
        Mesh baked = new Mesh();
        skinned.BakeMesh(baked);
        baked.SetUVs(GhostPalette.SlotChannel, palette.Slots(baked.vertexCount));
        bakedMeshes.Add(baked);
        return baked;
    }

    private int FindAnchor(Transform transform, Transform body)
    {
        for (Transform t = transform; t != null && t != body; t = t.parent)
        {
            for (int i = 0; i < anchorSources.Count; i++)
            {
                if (anchorSources[i] == t)
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
