using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class UmbrellaAnimation
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

    public static UmbrellaAnimation Find(Transform vehicle, Transform rigSource, CopiedRig rig)
    {
        GliderController glider = vehicle.GetComponentInChildren<GliderController>(true);
        if (glider == null || glider.umbrella == null || rig == null || !glider.umbrella.transform.IsChildOf(rigSource))
            return null;

        SkinnedMeshRenderer umbrella = glider.umbrella;
        Transform copy = TransformPaths.Find(rig.Root, TransformPaths.RelativePath(umbrella.transform, rigSource));
        if (copy == null || umbrella.sharedMesh.blendShapeCount == 0)
            return null;

        return new UmbrellaAnimation(copy.GetComponent<SkinnedMeshRenderer>(), umbrella.GetBlendShapeWeight(0), glider.umbrellaAnimSpeed);
    }

    public void Update(bool open, float deltaTime)
    {
        weight = Mathf.MoveTowards(weight, open ? openWeight : closedWeight, speed * deltaTime);
        for (int i = 0; i < blendShapeCount; i++)
            canopy.SetBlendShapeWeight(i, weight);
    }
}
