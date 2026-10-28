using System.Collections.Generic;

namespace BetterGhosts;

public static class GhostMotions
{
    public static List<IGhostMotion> Find(GhostBuild build)
    {
        List<IGhostMotion> motions = new();
        JumpPitch.Find(build, motions);
        InferredWheel.Find(build, motions);
        SpinningPart.Find(build, motions);
        HamsterWheel.Find(build, motions);
        LeaningPart.Find(build, motions);
        SteeredPart.Find(build, motions);
        SpinHandles.Find(build, motions);
        RetractingSpikes.Find(build, motions);
        HeldObjects.Find(build, motions);
        AnimatorDriver.Find(build, motions);
        LegKick.Find(build, motions);
        UmbrellaAnimation.Find(build, motions);
        WingFlaps.Find(build, motions);
        PorcarpineBall.Find(build, motions);
        OverworldAnimator.Find(build, motions);
        SwingingPivot.Find(build, motions);
        RiderPose.Find(build, motions);
        KiteFlight.Find(build, motions);
        StretchedLine.Find(build, motions);
        PrinterPaper.Find(build, motions);
        BoostStreams.Find(build, motions);
        return motions;
    }
}
