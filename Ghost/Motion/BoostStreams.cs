using System.Collections.Generic;
using Il2CppSpeed.Level;
using UnityEngine;

namespace BetterGhosts;

public class BoostStreams : IGhostMotion
{
    private readonly List<ParticleSystem> streams = new();
    private readonly float rate;
    private bool active;

    private BoostStreams(float rate)
    {
        this.rate = rate;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        WaterCoolerController controller = build.Vehicle.GetComponentInChildren<WaterCoolerController>(true);
        if (controller == null)
            return;

        BoostStreams boostStreams = new BoostStreams(controller.particlesEmission);
        for (int i = 0; i < controller.particlesInterface.Count; i++)
        {
            ParticleSystem source = controller.particlesInterface[i].particles;
            if (source == null || !source.gameObject.activeInHierarchy)
                continue;

            ParticleSystem stream = CopyStream(build, source);
            if (stream != null)
                boostStreams.streams.Add(stream);
        }

        if (boostStreams.streams.Count > 0)
            motions.Add(boostStreams);
    }

    private static ParticleSystem CopyStream(GhostBuild build, ParticleSystem source)
    {
        bool inRig = build.IsInRig(source.transform);
        if (inRig != (build.Rig != null))
            return null;

        ParticleSystem stream = ParticleCopy.Create(source, build.Pivots.Layer);
        if (inRig)
        {
            stream.transform.SetParent(build.Rig.CopyOf(source.transform.parent), false);
            stream.transform.localPosition = source.transform.localPosition;
            stream.transform.localRotation = source.transform.localRotation;
            stream.transform.localScale = source.transform.localScale;
        }
        else
        {
            build.Pivots.Place(stream.transform, source.transform);
        }

        SetEmission(stream, 0f);
        stream.Play(true);
        return stream;
    }

    public void Update(GhostFrame frame)
    {
        if (frame.ActionPressed == active)
            return;

        active = frame.ActionPressed;
        foreach (ParticleSystem stream in streams)
            SetEmission(stream, active ? rate : 0f);
    }

    private static void SetEmission(ParticleSystem stream, float rate)
    {
        ParticleSystem.EmissionModule emission = stream.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(rate);
    }
}
