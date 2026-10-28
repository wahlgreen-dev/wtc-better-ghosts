using System.Collections.Generic;
using Il2CppTriband.Speed;
using UnityEngine;

namespace BetterGhosts;

public class RetractingSpikes : IGhostMotion
{
    private readonly List<(Transform pivot, Vector3 shown, Vector3 hidden)> spikes = new();
    private readonly float speed;

    private RetractingSpikes(float speed)
    {
        this.speed = speed;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        var sources = build.Vehicle.GetComponentsInChildren<RetractableSpike>(false);
        if (sources.Length == 0)
            return;

        RetractingSpikes retracting = new RetractingSpikes(sources[0].speed);
        for (int i = 0; i < sources.Length; i++)
        {
            RetractableSpike source = sources[i];
            if (source.spike == null || source.hiddenPos == null || build.MovingPartFor(source.spike.parent) == null)
                continue;

            Transform pivot = build.MovingPartFor(source.spike);
            Vector3 hidden = source.spike.parent.InverseTransformPoint(source.hiddenPos.position);
            retracting.spikes.Add((pivot, source.defaultPos, hidden));
        }

        if (retracting.spikes.Count > 0)
            motions.Add(retracting);
    }

    public void Update(GhostFrame frame)
    {
        float blend = frame.DeltaTime > 0f ? 1f - Mathf.Exp(-speed * frame.DeltaTime) : 1f;
        foreach ((Transform pivot, Vector3 shown, Vector3 hidden) in spikes)
            pivot.localPosition = Vector3.Lerp(pivot.localPosition, frame.ActionPressed ? hidden : shown, blend);
    }
}
