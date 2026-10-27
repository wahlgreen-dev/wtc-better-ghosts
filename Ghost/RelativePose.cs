using UnityEngine;

namespace BetterGhosts;

public static class RelativePose
{
    public static void Copy(Transform copy, Transform source, Transform anchor)
    {
        copy.localPosition = anchor.InverseTransformPoint(source.position);
        copy.localRotation = Quaternion.Inverse(anchor.rotation) * source.rotation;
        Vector3 sourceScale = source.lossyScale;
        Vector3 anchorScale = anchor.lossyScale;
        copy.localScale = new Vector3(sourceScale.x / anchorScale.x, sourceScale.y / anchorScale.y, sourceScale.z / anchorScale.z);
    }
}
