using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class SteeredPart : IGhostMotion
{
    private readonly Transform pivot;
    private readonly Quaternion restRotation;
    private readonly Vector3 axis;
    private readonly float maxAngle;
    private readonly float rotateSpeed;
    private float angle;

    private SteeredPart(Transform pivot, RotateWithInput source)
    {
        this.pivot = pivot;
        restRotation = pivot.localRotation;
        axis = source.axis;
        maxAngle = source.maxAngle;
        rotateSpeed = source.rotateSpeed;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        List<RotateWithInput> steerers = build.LiveComponents<RotateWithInput>();
        for (int i = 0; i < steerers.Count; i++)
        {
            Transform pivot = build.MovingPartFor(steerers[i].transform);
            if (pivot != null)
                motions.Add(new SteeredPart(pivot, steerers[i]));
        }
    }

    public void Update(GhostFrame frame)
    {
        angle = Mathf.MoveTowards(angle, frame.Steering * maxAngle, rotateSpeed * frame.DeltaTime);
        pivot.localRotation = restRotation * Quaternion.AngleAxis(angle, axis);
    }
}
