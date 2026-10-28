using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class HamsterWheel : IGhostMotion
{
    private const float MeasuredSpinPerSpeed = 8.9f;

    private readonly Transform pivot;
    private readonly float degreesPerMeter;

    private HamsterWheel(Transform pivot, HamsterWheelController controller)
    {
        this.pivot = pivot;
        degreesPerMeter = MeasuredSpinPerSpeed / controller.wheelRadius;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        HamsterWheelController controller = build.Vehicle.GetComponentInChildren<HamsterWheelController>(true);
        if (controller == null || controller.wheelRotateHandle == null || controller.wheelRadius <= 0f)
            return;

        Transform pivot = build.MovingPartFor(controller.wheelRotateHandle);
        if (pivot != null)
            motions.Add(new HamsterWheel(pivot, controller));
    }

    public void Update(GhostFrame frame)
    {
        float direction = Vector3.Dot(frame.Velocity, frame.Forward) < 0f ? -1f : 1f;
        pivot.Rotate(Vector3.right, direction * frame.Speed * degreesPerMeter * frame.DeltaTime, Space.Self);
    }
}
