using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class JumpPitch : IGhostMotion
{
    private const float MinForwardSpeed = 0.5f;

    private readonly Transform pivot;
    private readonly Quaternion restRotation;
    private readonly float smoothTime;
    private float angle;
    private float angleVelocity;

    private JumpPitch(Transform pivot, SharCarController source)
    {
        this.pivot = pivot;
        restRotation = pivot.localRotation;
        smoothTime = source.jumpRotationSmoothTime;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        SharCarController shark = build.Vehicle.GetComponentInChildren<SharCarController>(true);
        if (shark == null || shark.visuals == null)
            return;

        Transform pivot = build.MovingPartFor(shark.visuals);
        if (pivot != null)
            motions.Add(new JumpPitch(pivot, shark));
    }

    public void Update(GhostFrame frame)
    {
        Vector3 localVelocity = Quaternion.Inverse(frame.Rotation) * frame.Velocity;
        float target = -Mathf.Atan2(localVelocity.y, Mathf.Max(localVelocity.z, MinForwardSpeed)) * Mathf.Rad2Deg;
        if (frame.DeltaTime > 0f)
            angle = Mathf.SmoothDamp(angle, target, ref angleVelocity, smoothTime, Mathf.Infinity, frame.DeltaTime);
        pivot.localRotation = restRotation * Quaternion.AngleAxis(angle, Vector3.right);
    }
}
