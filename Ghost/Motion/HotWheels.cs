using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;
using TransformList = Il2CppSystem.Collections.Generic.List<UnityEngine.Transform>;

namespace BetterGhosts;

public class HotWheels : IGhostMotion
{
    private readonly List<(Transform pivot, float radius)> wheels = new();
    private readonly float actionSpinMultiplier;

    private HotWheels(HotWheelsController controller)
    {
        actionSpinMultiplier = controller.actionButtonSpinMultiplier;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        HotWheelsController controller = build.Vehicle.GetComponentInChildren<HotWheelsController>(true);
        if (controller == null)
            return;

        HotWheels hotWheels = new HotWheels(controller);
        hotWheels.AddWheels(build, controller.wheelSpinHandlesFront, controller.wheelRadiusFront);
        hotWheels.AddWheels(build, controller.wheelSpinHandlesBack, controller.wheelRadiusBack);
        if (hotWheels.wheels.Count > 0)
            motions.Add(hotWheels);
    }

    public void Update(GhostFrame frame)
    {
        float speed = Vector3.Dot(frame.Velocity, frame.Forward) * (frame.ActionPressed ? actionSpinMultiplier : 1f);
        foreach ((Transform pivot, float radius) in wheels)
            pivot.Rotate(Vector3.right, speed / radius * Mathf.Rad2Deg * frame.DeltaTime, Space.Self);
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
