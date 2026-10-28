using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class UmbrellaAnimation : IGhostMotion
{
    private const float FullyOpen = 0f;
    private const float FullyClosed = 100f;

    private readonly SkinnedMeshRenderer canopy;
    private readonly int blendShapeCount;
    private readonly float closedWeight;
    private readonly float openWeight;
    private readonly float speed;
    private float weight;

    private UmbrellaAnimation(SkinnedMeshRenderer canopy, float closedWeight, float speed)
    {
        this.canopy = canopy;
        this.closedWeight = closedWeight;
        this.speed = speed;
        blendShapeCount = canopy.sharedMesh.blendShapeCount;
        openWeight = closedWeight > (FullyOpen + FullyClosed) / 2f ? FullyOpen : FullyClosed;
        weight = closedWeight;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        GliderController glider = build.Vehicle.GetComponentInChildren<GliderController>(true);
        if (glider == null || glider.umbrella == null || !build.IsInRig(glider.umbrella.transform))
            return;

        SkinnedMeshRenderer umbrella = glider.umbrella;
        Transform copy = build.MovingPartFor(umbrella.transform);
        if (copy == null || umbrella.sharedMesh.blendShapeCount == 0)
            return;

        motions.Add(new UmbrellaAnimation(copy.GetComponent<SkinnedMeshRenderer>(), umbrella.GetBlendShapeWeight(0), glider.umbrellaAnimSpeed));
    }

    public void Update(GhostFrame frame)
    {
        weight = Mathf.MoveTowards(weight, frame.ActionPressed ? openWeight : closedWeight, speed * frame.DeltaTime);
        for (int i = 0; i < blendShapeCount; i++)
            canopy.SetBlendShapeWeight(i, weight);
    }
}
