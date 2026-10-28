using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class PrinterPaper : IGhostMotion
{
    private readonly ParticleSystem paper;
    private readonly float burstInterval;
    private readonly float burstDuration;
    private readonly float burstIntervalBoosting;
    private readonly float burstDurationBoosting;
    private bool bursting;
    private float lastBurstStart;
    private float lastBurstEnd;

    private PrinterPaper(ParticleSystem paper, CarAndPrinterController source)
    {
        this.paper = paper;
        burstInterval = source.burstInterval;
        burstDuration = source.burstDuration;
        burstIntervalBoosting = source.burstIntervalBoosting;
        burstDurationBoosting = source.burstDurationBoosting;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        if (build.Rig != null)
            return;

        var sources = build.Vehicle.GetComponentsInChildren<CarAndPrinterController>(false);
        for (int i = 0; i < sources.Length; i++)
        {
            CarAndPrinterController source = sources[i];
            if (source.particles == null || build.IsInRig(source.particles.transform))
                continue;

            motions.Add(new PrinterPaper(CopyPaper(build, source.particles), source));
        }
    }

    private static ParticleSystem CopyPaper(GhostBuild build, ParticleSystem source)
    {
        ParticleSystem paper = ParticleCopy.Create(source, build.Pivots.Layer);
        build.Pivots.Place(paper.transform, source.transform);
        build.Palette.PaintWithoutSlots(paper.GetComponent<ParticleSystemRenderer>(), 1);
        paper.Play(true);
        return paper;
    }

    public void Update(GhostFrame frame)
    {
        float interval = frame.ActionPressed ? burstIntervalBoosting : burstInterval;
        float duration = frame.ActionPressed ? burstDurationBoosting : burstDuration;
        if (bursting && frame.Time - lastBurstStart >= duration)
        {
            bursting = false;
            lastBurstEnd = frame.Time;
        }
        else if (!bursting && frame.Time - lastBurstEnd >= interval)
        {
            bursting = true;
            lastBurstStart = frame.Time;
            paper.Emit(1);
        }
    }
}
