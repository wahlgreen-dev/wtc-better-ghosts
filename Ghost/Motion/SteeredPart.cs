using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class SteeredPart
{
    private readonly Transform pivot;
    private readonly Quaternion restRotation;
    private readonly Vector3 axis;
    private readonly float maxAngle;
    private readonly float rotateSpeed;
    private float angle;

    public SteeredPart(Transform pivot, RotateWithInput source)
    {
        this.pivot = pivot;
        restRotation = pivot.localRotation;
        axis = source.axis;
        maxAngle = source.maxAngle;
        rotateSpeed = source.rotateSpeed;
    }

    public void Update(float steering, float deltaTime)
    {
        angle = Mathf.MoveTowards(angle, steering * maxAngle, rotateSpeed * deltaTime);
        pivot.localRotation = restRotation * Quaternion.AngleAxis(angle, axis);
    }
}
