using System.Collections.Generic;
using Il2Cpp;
using Il2CppTriband.Speed;
using UnityEngine;
using TransformList = Il2CppSystem.Collections.Generic.List<UnityEngine.Transform>;

namespace BetterGhosts;

public class SpinHandles : IGhostMotion
{
    private readonly List<(Transform pivot, float radius)> wheels = new();
    private readonly float actionSpinMultiplier;

    private SpinHandles(float actionSpinMultiplier)
    {
        this.actionSpinMultiplier = actionSpinMultiplier;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        HotWheelsController hotWheels = build.Vehicle.GetComponentInChildren<HotWheelsController>(true);
        if (hotWheels != null)
            Add(build, motions, hotWheels.wheelSpinHandlesFront, hotWheels.wheelRadiusFront, hotWheels.wheelSpinHandlesBack, hotWheels.wheelRadiusBack, hotWheels.actionButtonSpinMultiplier);

        SpinningWheels spinning = build.Vehicle.GetComponentInChildren<SpinningWheels>(true);
        if (spinning != null)
            Add(build, motions, spinning.wheelSpinHandlesFront, spinning.wheelRadiusFront, spinning.wheelSpinHandlesBack, spinning.wheelRadiusBack, spinning.actionButtonSpinMultiplier);
    }

    public void Update(GhostFrame frame)
    {
        float speed = Vector3.Dot(frame.Velocity, frame.Forward) * (frame.ActionPressed ? actionSpinMultiplier : 1f);
        foreach ((Transform pivot, float radius) in wheels)
            pivot.Rotate(Vector3.right, speed / radius * Mathf.Rad2Deg * frame.DeltaTime, Space.Self);
    }

    private static void Add(GhostBuild build, List<IGhostMotion> motions, TransformList front, float frontRadius, TransformList back, float backRadius, float actionSpinMultiplier)
    {
        SpinHandles handles = new SpinHandles(actionSpinMultiplier);
        handles.AddWheels(build, front, frontRadius);
        handles.AddWheels(build, back, backRadius);
        if (handles.wheels.Count > 0)
            motions.Add(handles);
    }

    private void AddWheels(GhostBuild build, TransformList handles, float radius)
    {
        if (handles == null || radius <= 0f)
            return;

        for (int i = 0; i < handles.Count; i++)
        {
            Transform pivot = handles[i] != null ? build.MovingPartFor(handles[i]) : null;
            if (pivot != null)
                wheels.Add((pivot, radius));
        }
    }
}
