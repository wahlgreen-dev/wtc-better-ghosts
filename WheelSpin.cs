using UnityEngine;

namespace BetterGhosts;

public class WheelSpin
{
    public readonly string Path;
    public readonly Vector3 Axis;
    public readonly Vector3 Anchor;
    public readonly Vector3 ReferencePosition;
    public readonly Quaternion ReferenceRotation;

    public WheelSpin(string path, Vector3 axis, Vector3 anchor, Vector3 referencePosition, Quaternion referenceRotation)
    {
        Path = path;
        Axis = axis;
        Anchor = anchor;
        ReferencePosition = referencePosition;
        ReferenceRotation = referenceRotation;
    }

    public static WheelSpin Measure(Transform wheel, HingeJoint hinge, Transform body, Transform vehicle)
    {
        Quaternion bodyRotationInverse = Quaternion.Inverse(body.rotation);
        Vector3 axis = (bodyRotationInverse * (wheel.rotation * hinge.axis)).normalized;
        Vector3 anchor = body.InverseTransformPoint(wheel.TransformPoint(hinge.anchor));
        return new WheelSpin(TransformPaths.RelativePath(wheel, vehicle), axis, anchor, body.InverseTransformPoint(wheel.position), bodyRotationInverse * wheel.rotation);
    }

    public void Pose(float angle, out Vector3 position, out Quaternion rotation)
    {
        Quaternion turn = Quaternion.AngleAxis(angle, Axis);
        rotation = turn * ReferenceRotation;
        position = Anchor + turn * (ReferencePosition - Anchor);
    }
}
