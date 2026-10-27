using System;
using System.Collections.Generic;
using UnityEngine;

namespace BetterGhosts;

public class SampleCursor
{
    private readonly IReadOnlyList<float> times;
    private int index;

    public SampleCursor(IReadOnlyList<float> times)
    {
        this.times = times;
    }

    public void Seek(float time, out int from, out int to, out float blend)
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
}
