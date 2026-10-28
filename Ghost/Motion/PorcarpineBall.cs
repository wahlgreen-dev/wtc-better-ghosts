using System.Collections.Generic;
using Il2CppTriband.Speed;
using UnityEngine;

namespace BetterGhosts;

public class PorcarpineBall : IGhostMotion
{
    private readonly Animator animator;
    private readonly Transform spinHandle;
    private readonly float spinPerSpeed;
    private readonly int ballHash;
    private readonly int squashHash;
    private readonly int squashToStretchHash;
    private readonly float squashToStretch;
    private bool wasGrounded = true;

    private PorcarpineBall(Animator animator, Transform spinHandle, PorcarpineController source)
    {
        this.animator = animator;
        this.spinHandle = spinHandle;
        spinPerSpeed = source.velToSpinFactor;
        AnimationCurve velocityToSquashStretch = source.velToSquashStretch;
        squashToStretch = velocityToSquashStretch.Evaluate(velocityToSquashStretch.keys[velocityToSquashStretch.length - 1].time);
        ballHash = Animator.StringToHash(PorcarpineController.porcupineParamName);
        squashHash = Animator.StringToHash(PorcarpineController.squashParamName);
        squashToStretchHash = Animator.StringToHash(PorcarpineController.squashToStretchParamName);
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        PorcarpineController source = build.Vehicle.GetComponentInChildren<PorcarpineController>(true);
        if (source == null || source.animator == null || build.Rig == null || !build.IsInRig(source.animator.transform))
            return;

        Animator animator = build.Rig.CopyOf(source.animator.transform).GetComponent<Animator>();
        Transform spinHandle = source.spinHandle != null && build.IsInRig(source.spinHandle) ? build.Rig.CopyOf(source.spinHandle) : null;
        if (animator != null)
            motions.Add(new PorcarpineBall(animator, spinHandle, source));
    }

    public void Update(GhostFrame frame)
    {
        animator.SetBool(ballHash, frame.ActionPressed);
        animator.SetFloat(squashToStretchHash, squashToStretch);
        if (frame.IsGrounded && !wasGrounded)
            animator.SetTrigger(squashHash);
        wasGrounded = frame.IsGrounded;

        if (spinHandle != null)
            spinHandle.Rotate(Vector3.right, Vector3.Dot(frame.Velocity, frame.Forward) * spinPerSpeed * frame.DeltaTime, Space.Self);
    }
}
