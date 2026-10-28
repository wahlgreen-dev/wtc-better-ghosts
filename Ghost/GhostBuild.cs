using UnityEngine;

namespace BetterGhosts;

public class GhostBuild
{
    public readonly Transform Vehicle;
    public readonly Transform Body;
    public readonly Transform Root;
    public readonly VehicleSetup Setup;
    public readonly GhostSamples Samples;
    public readonly GhostPivots Pivots;
    public readonly GhostPalette Palette;

    public GhostBuild(Transform vehicle, Transform body, Transform root, VehicleSetup setup, GhostSamples samples, GhostPivots pivots, GhostPalette palette)
    {
        Vehicle = vehicle;
        Body = body;
        Root = root;
        Setup = setup;
        Samples = samples;
        Pivots = pivots;
        Palette = palette;
    }

    public CopiedRig Rig { get; private set; }

    public void UseRig(CopiedRig rig)
    {
        Rig = rig;
    }

    public bool IsInRig(Transform source)
    {
        return Setup.RigRoot != null && source.IsChildOf(Setup.RigRoot);
    }

    public Transform MovingPartFor(Transform source)
    {
        if (Rig == null)
            return IsInRig(source) ? null : Pivots.Create(source);
        return IsInRig(source) ? Rig.CopyOf(source) : null;
    }
}
