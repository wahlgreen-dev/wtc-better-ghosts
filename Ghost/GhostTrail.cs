using UnityEngine;

namespace BetterGhosts;

public class GhostTrail
{
    private const string TrailName = "Effect_GhostCarTrail";

    private readonly Transform trail;
    private readonly Transform originalParent;
    private readonly Vector3 originalPosition;
    private readonly Quaternion originalRotation;
    private readonly Vector3 originalScale;

    public GhostTrail(Transform ghostTransform, Transform modelTransform, Transform root)
    {
        trail = TransformPaths.FindDescendant(modelTransform, TrailName);
        originalParent = trail.parent;
        originalPosition = trail.localPosition;
        originalRotation = trail.localRotation;
        originalScale = trail.localScale;

        Vector3 position = originalPosition;
        Quaternion rotation = originalRotation;
        for (Transform parent = originalParent; parent != ghostTransform; parent = parent.parent)
        {
            position = parent.localPosition + parent.localRotation * Vector3.Scale(parent.localScale, position);
            rotation = parent.localRotation * rotation;
        }

        Vector3 rootScale = root.localScale;
        trail.SetParent(root, false);
        trail.localPosition = new Vector3(position.x / rootScale.x, position.y / rootScale.y, position.z / rootScale.z);
        trail.localRotation = rotation;
        trail.localScale = new Vector3(1f / rootScale.x, 1f / rootScale.y, 1f / rootScale.z);
    }

    public void Restore()
    {
        if (trail == null || originalParent == null)
            return;

        trail.SetParent(originalParent, false);
        trail.localPosition = originalPosition;
        trail.localRotation = originalRotation;
        trail.localScale = originalScale;
    }
}
