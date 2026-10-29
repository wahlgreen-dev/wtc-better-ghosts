using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class SpinningPart : IGhostMotion
{
    private readonly Transform transform;
    private readonly Vector3 axis;
    private readonly float speed;
    private readonly Space space;
    private readonly bool speedBasedOnVelocity;

    private SpinningPart(Transform transform, RotateAroundAxis source)
    {
        this.transform = transform;
        axis = source.axis;
        speed = source.speed * source.speedMod;
        space = source.space;
        speedBasedOnVelocity = source.speedBasedOnVelocity;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        List<RotateAroundAxis> rotators = build.LiveComponents<RotateAroundAxis>();
        for (int i = 0; i < rotators.Count; i++)
        {
            Transform part = build.MovingPartFor(rotators[i].transform);
            if (part != null)
                motions.Add(new SpinningPart(part, rotators[i]));
        }
    }

    public void Update(GhostFrame frame)
    {
        float currentSpeed = speedBasedOnVelocity ? speed * frame.Speed : speed;
        transform.Rotate(axis * (currentSpeed * frame.DeltaTime), space);
    }
}
