using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class HamsterWheel
{
    private const float MeasuredSpinPerSpeed = 8.9f;

    private readonly Transform pivot;
    private readonly float degreesPerMeter;

    public HamsterWheel(Transform pivot, HamsterWheelController controller)
    {
        this.pivot = pivot;
        degreesPerMeter = MeasuredSpinPerSpeed / controller.wheelRadius;
    }

    public static HamsterWheelController Find(Transform vehicle)
    {
        HamsterWheelController controller = vehicle.GetComponentInChildren<HamsterWheelController>(true);
        if (controller == null || controller.wheelRotateHandle == null || controller.wheelRadius <= 0f)
            return null;
        return controller;
    }

    public void Update(Vector3 velocity, Vector3 forward, float deltaTime)
    {
        float direction = Vector3.Dot(velocity, forward) < 0f ? -1f : 1f;
        pivot.Rotate(Vector3.right, direction * velocity.magnitude * degreesPerMeter * deltaTime, Space.Self);
    }
}
