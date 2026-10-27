using UnityEngine;

namespace BetterGhosts;

public class RiderPose
{
    private const float MinimumTurningSpeed = 0.5f;
    private const float TurnRate = 8f;

    private readonly Transform pivot;
    private readonly Vector3 offset;
    private readonly Quaternion modelRotation;
    private Quaternion facing;

    public RiderPose(Transform pivot, Transform rider, Transform body)
    {
        this.pivot = pivot;
        offset = rider.position - body.position;
        facing = Quaternion.LookRotation(Flatten(rider.forward, Vector3.forward), Vector3.up);
        modelRotation = Quaternion.Inverse(facing) * rider.rotation;
    }

    public void Update(Vector3 bodyPosition, Vector3 velocity, float deltaTime)
    {
        Vector3 horizontal = new Vector3(velocity.x, 0f, velocity.z);
        if (horizontal.magnitude > MinimumTurningSpeed)
            facing = Quaternion.Slerp(facing, Quaternion.LookRotation(horizontal, Vector3.up), 1f - Mathf.Exp(-TurnRate * deltaTime));

        pivot.position = bodyPosition + offset;
        pivot.rotation = facing * modelRotation;
    }

    private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
    {
        Vector3 flat = new Vector3(direction.x, 0f, direction.z);
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : fallback;
    }
}
