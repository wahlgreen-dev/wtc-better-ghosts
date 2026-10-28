using System.Collections.Generic;

namespace BetterGhosts;

public static class GhostMotions
{
    public static List<IGhostMotion> Find(GhostBuild build)
    {
        List<IGhostMotion> motions = new();
        InferredWheel.Find(build, motions);
        SpinningPart.Find(build, motions);
        HamsterWheel.Find(build, motions);
        LeaningPart.Find(build, motions);
        SteeredPart.Find(build, motions);
        HotWheels.Find(build, motions);
        AnimatorDriver.Find(build, motions);
        UmbrellaAnimation.Find(build, motions);
        SwingingPivot.Find(build, motions);
        RiderPose.Find(build, motions);
        return motions;
    }
}
