using System.Collections.Generic;
using Il2CppSpeed.GhostCar;
using UnityEngine;

namespace BetterGhosts;

public class ShapedGhost
{
    private const string GenericModelName = "GhostCarModel";
    private const string GenericBodyName = "Body";

    private readonly GameObject genericModel;
    private readonly GameObject root;
    private readonly GhostPalette palette;
    private readonly GhostSamples samples;
    private readonly ShapeCopier shape;
    private readonly CopiedRig rig;
    private readonly List<IGhostMotion> motions = new();

    public ShapedGhost(GhostCarManager manager, Transform vehicle, Transform body, Transform[] parts, Transform[] goalObjects, VehicleSetup setup, PartRecording recording)
    {
        Transform modelTransform = TransformPaths.FindDescendant(manager.ghostTransform, GenericModelName);
        Renderer ghostRenderer = TransformPaths.FindDescendant(modelTransform, GenericBodyName).GetComponent<Renderer>();
        int layer = ghostRenderer.gameObject.layer;
        palette = new GhostPalette(ghostRenderer);
        samples = new GhostSamples(manager.existingGhostData);

        root = new GameObject("BetterGhost");
        root.layer = layer;
        root.transform.SetParent(modelTransform.parent, false);
        root.transform.localScale = body.lossyScale;
        root.transform.SetPositionAndRotation(samples.Positions[0], samples.Rotations[0]);

        GhostPivots pivots = new GhostPivots(root.transform, body, layer);
        GhostBuild build = new GhostBuild(vehicle, body, root.transform, setup, samples, pivots, palette);
        PowerEffects power = new PowerEffects();

        RecordedParts recordedParts = RecordedParts.Create(build, parts, recording);
        if (recordedParts != null)
            motions.Add(recordedParts);
        motions.AddRange(GhostMotions.Find(build));

        shape = new ShapeCopier(palette, pivots, power);
        shape.Copy(build);

        if (setup.RigRoot != null)
        {
            rig = new CopiedRig(setup.RigRoot, setup.RigKeepsPhysics, setup.RigStartsAtRest, pivots, palette, power);
            build.UseRig(rig);
            motions.AddRange(GhostMotions.Find(build));
        }

        motions.AddRange(GoalObjectGhost.CreateAll(build, goalObjects, recording, palette));
        motions.Add(power);

        genericModel = modelTransform.gameObject;
        genericModel.SetActive(false);
    }

    public void Update(float time, float deltaTime)
    {
        GhostFrame frame = samples.At(time, deltaTime);
        root.transform.SetPositionAndRotation(frame.Position, frame.Rotation);
        foreach (IGhostMotion motion in motions)
            motion.Update(frame);
        rig?.Update(root.activeInHierarchy);
    }

    public void Destroy()
    {
        if (genericModel != null)
            genericModel.SetActive(true);
        if (root != null)
            Object.Destroy(root);

        rig?.Destroy();
        shape.Destroy();
        palette.Destroy();
    }
}
