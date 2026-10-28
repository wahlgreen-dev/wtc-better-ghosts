using System.Collections.Generic;
using Il2CppSpeed.Overworld;
using UnityEngine;

namespace BetterGhosts;

public class RiderPose : IGhostMotion
{
    public const string RiderName = "Car visuals moved by code";

    private const float MinimumTurningSpeed = 0.5f;
    private const float TurnRate = 8f;

    private readonly Transform copy;
    private readonly Vector3 offset;
    private readonly Quaternion modelRotation;
    private Quaternion facing;

    private RiderPose(Transform copy, Transform rider, Transform body, bool upright)
    {
        this.copy = copy;
        offset = rider.position - body.position;
        facing = Quaternion.LookRotation(Flatten(rider.forward, Vector3.forward), Vector3.up);
        modelRotation = upright ? Quaternion.identity : Quaternion.Inverse(facing) * rider.rotation;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        Transform rider = TransformPaths.Find(build.Vehicle, RiderName);
        Transform overworldModel = rider == null ? OverworldModel(build.Vehicle) : null;
        Transform source = rider ?? overworldModel;
        if (source == null)
            return;

        Transform copy = build.MovingPartFor(source);
        if (copy != null)
            motions.Add(new RiderPose(copy, source, build.Body, overworldModel != null));
    }

    public void Update(GhostFrame frame)
    {
        Vector3 horizontal = new Vector3(frame.Velocity.x, 0f, frame.Velocity.z);
        if (horizontal.magnitude > MinimumTurningSpeed)
            facing = Quaternion.Slerp(facing, Quaternion.LookRotation(horizontal, Vector3.up), 1f - Mathf.Exp(-TurnRate * frame.DeltaTime));

        copy.position = frame.Position + offset;
        copy.rotation = facing * modelRotation;
    }

    private static Transform OverworldModel(Transform vehicle)
    {
        OverworldPlayerMovementController controller = vehicle.GetComponentInChildren<OverworldPlayerMovementController>(true);
        return controller != null ? controller.model : null;
    }

    private static Vector3 Flatten(Vector3 direction, Vector3 fallback)
    {
        Vector3 flat = new Vector3(direction.x, 0f, direction.z);
        return flat.sqrMagnitude > 0.0001f ? flat.normalized : fallback;
    }
}
