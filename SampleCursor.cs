using System.Collections.Generic;

namespace BetterGhosts;

public class SampleCursor
{
    readonly IReadOnlyList<float> times;
    int index;

    public SampleCursor(IReadOnlyList<float> times)
    {
        this.times = times;
    }

    public bool Seek(float time, out int from, out int to, out float blend)
    {
        from = to = 0;
        blend = 0f;
        if (times.Count == 0)
            return false;

        if (index >= times.Count || time < times[index])
            index = 0;
        while (index + 1 < times.Count && times[index + 1] <= time)
            index++;

        from = index;
        to = System.Math.Min(index + 1, times.Count - 1);
        var span = times[to] - times[from];
        blend = span > 0f ? UnityEngine.Mathf.Clamp01((time - times[from]) / span) : 0f;
        return true;
    }
}
