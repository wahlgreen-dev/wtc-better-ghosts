using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class KiteFlight : IGhostMotion
{
    private const float TargetLeadAhead = 3.8f;
    private const float ProgressAverageSeconds = 1f;
    private const float TiltBehindCar = 37f;

    private readonly Transform kite;
    private readonly Vector3 lineOrigin;
    private readonly Quaternion courseRotation;
    private readonly Vector3 kiteOffset;
    private readonly float smoothTime;
    private readonly float maxSpeed;
    private readonly float rotationSmoothTime;
    private readonly float[] progress;
    private Vector3 position;
    private Vector3 velocity;
    private bool placed;

    private KiteFlight(Transform kite, KiteSurferController source, GhostSamples samples)
    {
        this.kite = kite;
        lineOrigin = source.baseTarget.position;
        Vector3 course = source.finishLine.transform.position - source.kiteStartPos;
        course.y = 0f;
        courseRotation = Quaternion.LookRotation(course.normalized);
        kiteOffset = source.kiteOffset;
        smoothTime = source.kiteSmoothTime;
        maxSpeed = source.kiteMaxVel;
        rotationSmoothTime = source.kiteRotSmoothTime;
        progress = AveragedProgress(samples);
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        KiteSurferController source = build.Vehicle.GetComponentInChildren<KiteSurferController>(true);
        if (source == null || source.kiteVisuals == null || source.baseTarget == null || source.finishLine == null)
            return;

        Transform kite = build.MovingPartFor(source.kiteVisuals);
        if (kite != null)
            motions.Add(new KiteFlight(kite, source, build.Samples));
    }

    public void Update(GhostFrame frame)
    {
        float along = Mathf.Lerp(progress[frame.From], progress[frame.To], frame.Blend) + TargetLeadAhead;
        Vector3 target = lineOrigin + courseRotation * (Vector3.forward * along + kiteOffset);
        if (!placed || frame.DeltaTime <= 0f)
        {
            position = target;
            placed = true;
        }
        else
        {
            position = Vector3.SmoothDamp(position, target, ref velocity, smoothTime, maxSpeed, frame.DeltaTime);
        }

        Vector3 fromCar = Quaternion.Inverse(courseRotation) * (position - frame.Position);
        float turn = Mathf.Atan2(fromCar.x, fromCar.z) * Mathf.Rad2Deg;
        Quaternion rotation = courseRotation * Quaternion.Euler(TiltBehindCar * Mathf.Cos(turn * Mathf.Deg2Rad), turn, 0f);
        float rotationBlend = frame.DeltaTime > 0f ? 1f - Mathf.Exp(-frame.DeltaTime / rotationSmoothTime) : 1f;
        kite.SetPositionAndRotation(position, Quaternion.Slerp(kite.rotation, rotation, rotationBlend));
    }

    private float[] AveragedProgress(GhostSamples samples)
    {
        Vector3 forward = courseRotation * Vector3.forward;
        float[] raw = new float[samples.Count];
        for (int i = 0; i < raw.Length; i++)
            raw[i] = Vector3.Dot(samples.Positions[i] - lineOrigin, forward);

        float[] averaged = new float[raw.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            int count = 0;
            float sum = 0f;
            for (int j = 0; j < raw.Length; j++)
            {
                if (Mathf.Abs(samples.Times[j] - samples.Times[i]) > ProgressAverageSeconds)
                    continue;
                sum += raw[j];
                count++;
            }
            averaged[i] = sum / count;
        }
        return averaged;
    }
}
