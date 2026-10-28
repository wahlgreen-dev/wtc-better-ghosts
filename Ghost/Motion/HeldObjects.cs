using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class HeldObjects : IGhostMotion
{
    private class HeldObject
    {
        public Transform Pivot;
        public Vector3 LocalPosition;
        public Quaternion LocalRotation;
    }

    private readonly GhostBuild build;
    private readonly Transform followedSource;
    private readonly List<HeldObject> held = new();
    private Transform followed;

    private HeldObjects(GhostBuild build, Transform followedSource)
    {
        this.build = build;
        this.followedSource = followedSource;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        if (build.Rig != null)
            return;

        var followers = build.Vehicle.GetComponentsInChildren<FollowTransform>(false);
        var joints = build.Vehicle.GetComponentsInChildren<FixedJoint>(false);
        for (int i = 0; i < followers.Length; i++)
        {
            FollowTransform follower = followers[i];
            if (follower.follow == null || !build.IsInRig(follower.follow) || build.IsInRig(follower.transform))
                continue;

            HeldObjects heldObjects = new HeldObjects(build, follower.follow);
            heldObjects.Hold(follower.transform);
            Rigidbody holderBody = follower.GetComponent<Rigidbody>();
            for (int j = 0; j < joints.Length; j++)
            {
                if (holderBody != null && joints[j].connectedBody == holderBody && !build.IsInRig(joints[j].transform))
                    heldObjects.Hold(joints[j].transform);
            }
            motions.Add(heldObjects);
        }
    }

    public void Update(GhostFrame frame)
    {
        if (followed == null)
            followed = build.Rig?.CopyOf(followedSource);
        if (followed == null)
            return;

        foreach (HeldObject heldObject in held)
            heldObject.Pivot.SetPositionAndRotation(followed.TransformPoint(heldObject.LocalPosition), followed.rotation * heldObject.LocalRotation);
    }

    private void Hold(Transform source)
    {
        held.Add(new HeldObject
        {
            Pivot = build.Pivots.Create(source),
            LocalPosition = followedSource.InverseTransformPoint(source.position),
            LocalRotation = Quaternion.Inverse(followedSource.rotation) * source.rotation,
        });
    }
}
