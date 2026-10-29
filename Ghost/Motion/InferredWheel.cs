using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class InferredWheel : IGhostMotion
{
    private const float SpeedVariation = 0.1f;
    private const float DriftAmount = 0.1f;
    private const float DriftRate = 0.3f;
    private const float MovingSpeed = 1f;
    private const float SpinUpTime = 0.5f;

    private readonly Transform pivot;
    private readonly Vector3 axis;
    private readonly Vector3 anchorOffset;
    private readonly Vector3 restPosition;
    private readonly Quaternion restRotation;
    private readonly float speed;
    private readonly float driftSeed;
    private float currentSpeed;
    private float angle;

    private InferredWheel(Transform pivot, InputWheel wheel, HingeJoint hinge)
    {
        this.pivot = pivot;
        axis = hinge.axis.normalized;
        restPosition = pivot.localPosition;
        restRotation = pivot.localRotation;
        anchorOffset = Vector3.Scale(pivot.localScale, hinge.anchor);
        speed = wheel.maxAngularVelocity * Random.Range(1f - SpeedVariation, 1f + SpeedVariation);
        driftSeed = Random.Range(0f, 100f);
        angle = Random.Range(0f, 360f);
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        if (build.Setup.RecordedPartPaths.Length > 0)
            return;

        List<InputWheel> wheels = build.LiveComponents<InputWheel>();
        for (int i = 0; i < wheels.Count; i++)
        {
            HingeJoint hinge = wheels[i].GetComponent<HingeJoint>();
            if (hinge == null || build.IsInRig(wheels[i].transform))
                continue;

            Transform pivot = build.MovingPartFor(wheels[i].transform);
            if (pivot != null)
                motions.Add(new InferredWheel(pivot, wheels[i], hinge));
        }
    }

    public void Update(GhostFrame frame)
    {
        float drift = 1f + DriftAmount * (Mathf.PerlinNoise(driftSeed, frame.Time * DriftRate) * 2f - 1f);
        float targetSpeed = frame.Speed > MovingSpeed ? speed * drift : 0f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Mathf.Abs(speed) / SpinUpTime * frame.DeltaTime);
        angle += currentSpeed * frame.DeltaTime;

        Quaternion turn = Quaternion.AngleAxis(angle, axis);
        pivot.localRotation = restRotation * turn;
        pivot.localPosition = restPosition + restRotation * anchorOffset - restRotation * (turn * anchorOffset);
    }
}
