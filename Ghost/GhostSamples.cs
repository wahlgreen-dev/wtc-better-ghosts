using System;
using System.Collections.Generic;
using Il2CppSpeed.GhostCar;
using UnityEngine;

namespace BetterGhosts;

public class GhostSamples
{
    private readonly List<float> times = new();
    private readonly List<Vector3> positions = new();
    private readonly List<Quaternion> rotations = new();
    private readonly List<Vector3> velocities = new();
    private readonly List<bool> grounded = new();
    private readonly List<float> skids = new();
    private readonly List<float> inputs = new();
    private readonly List<bool> actionPressed = new();
    private int index;

    public GhostSamples(GhostCarData data)
    {
        var samples = data.ghostData;
        for (int i = 0; i < samples.Count; i++)
        {
            GhostCarInstantSaveData sample = samples[i];
            times.Add(sample.time);
            positions.Add(sample.position);
            rotations.Add(sample.rotation);
            velocities.Add(sample.velocity);
            grounded.Add(sample.isGrounded);
            skids.Add(sample.skid);
            inputs.Add(sample.input);
            actionPressed.Add(sample.actionPressed);
        }
    }

    public int Count => times.Count;

    public IReadOnlyList<float> Times => times;

    public IReadOnlyList<Vector3> Positions => positions;

    public IReadOnlyList<Quaternion> Rotations => rotations;

    public IReadOnlyList<Vector3> Velocities => velocities;

    public GhostFrame At(float time, float deltaTime)
    {
        Seek(time, out int from, out int to, out float blend);
        int nearest = blend < 0.5f ? from : to;
        return new GhostFrame
        {
            Time = time,
            DeltaTime = deltaTime,
            From = from,
            To = to,
            Blend = blend,
            Position = Vector3.Lerp(positions[from], positions[to], blend),
            Rotation = Quaternion.Slerp(rotations[from], rotations[to], blend),
            Velocity = Vector3.Lerp(velocities[from], velocities[to], blend),
            IsGrounded = grounded[nearest],
            Skid = Mathf.Lerp(skids[from], skids[to], blend),
            Steering = Mathf.Lerp(inputs[from], inputs[to], blend),
            TurnRate = TurnRate(from, to),
            ActionPressed = actionPressed[nearest],
        };
    }

    private void Seek(float time, out int from, out int to, out float blend)
    {
        if (time < times[index])
            index = 0;
        while (index + 1 < times.Count && times[index + 1] <= time)
            index++;

        from = index;
        to = Math.Min(index + 1, times.Count - 1);
        float span = times[to] - times[from];
        blend = span > 0f ? Mathf.Clamp01((time - times[from]) / span) : 0f;
    }

    private float TurnRate(int from, int to)
    {
        float span = times[to] - times[from];
        if (span <= 0f)
            return 0f;

        float yawChange = Mathf.DeltaAngle(rotations[from].eulerAngles.y, rotations[to].eulerAngles.y);
        return yawChange * Mathf.Deg2Rad / span;
    }
}
