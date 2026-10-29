using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class LeaningPart : IGhostMotion
{
    private const float LeanPerTurnRate = 0.22f;

    private readonly Transform pivot;
    private readonly Quaternion restRotation;
    private readonly float maxAngle;
    private readonly AnimationCurve leanToAngle;
    private readonly float rotateSpeed;
    private readonly float turnDirection;
    private readonly bool onlyIfGrounded;
    private readonly float maxInputContribution;
    private float angle;

    private LeaningPart(Transform pivot, LeanWithSkid source)
    {
        this.pivot = pivot;
        restRotation = pivot.localRotation;
        maxAngle = source.maxAngle;
        leanToAngle = source.leanToAngle;
        rotateSpeed = source.rotateSpeed;
        turnDirection = source.invert ? 1f : -1f;
        onlyIfGrounded = source.onlyIfGrounded;
        maxInputContribution = source.maxInputContribution;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        List<LeanWithSkid> leaners = build.LiveComponents<LeanWithSkid>();
        for (int i = 0; i < leaners.Count; i++)
        {
            Transform pivot = build.MovingPartFor(leaners[i].transform);
            if (pivot != null)
                motions.Add(new LeaningPart(pivot, leaners[i]));
        }
    }

    public void Update(GhostFrame frame)
    {
        float lean = Mathf.Clamp(turnDirection * LeanPerTurnRate * frame.TurnRate + frame.Steering * maxInputContribution, -1f, 1f);
        float target = onlyIfGrounded && !frame.IsGrounded ? 0f : Mathf.Sign(lean) * leanToAngle.Evaluate(Mathf.Abs(lean)) * maxAngle;
        angle = Mathf.MoveTowards(angle, target, rotateSpeed * frame.DeltaTime);
        pivot.localRotation = restRotation * Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
