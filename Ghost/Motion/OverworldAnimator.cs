using System.Collections.Generic;
using Il2CppSpeed.Overworld;
using UnityEngine;

namespace BetterGhosts;

public class OverworldAnimator : IGhostMotion
{
    private const string OnGroundParamName = "OnGround";
    private const string VerticalVelocityParamName = "yVelocity";
    private const string HideStateName = "CarHideInBush";
    private const string ExitStateName = "CarExitFromBush";
    private const float StandingStillSpeed = 0.5f;

    private readonly Animator animator;
    private readonly int onGroundHash = Animator.StringToHash(OnGroundParamName);
    private readonly int verticalVelocityHash = Animator.StringToHash(VerticalVelocityParamName);
    private readonly int hideHash = Animator.StringToHash(HideStateName);
    private readonly int exitHash = Animator.StringToHash(ExitStateName);
    private readonly bool hasOnGround;
    private readonly bool hasVerticalVelocity;
    private readonly bool hasBush;
    private float verticalVelocity;
    private bool hidden;

    private OverworldAnimator(Animator animator)
    {
        this.animator = animator;
        hasOnGround = HasParameter(animator, OnGroundParamName);
        hasVerticalVelocity = HasParameter(animator, VerticalVelocityParamName);
        hasBush = animator.HasState(0, hideHash) && animator.HasState(0, exitHash);
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        OverworldPlayerMovementController controller = build.Vehicle.GetComponentInChildren<OverworldPlayerMovementController>(true);
        if (controller == null || controller.model == null || build.Rig == null)
            return;

        Animator source = controller.model.GetComponentInChildren<Animator>(true);
        if (source == null || !build.IsInRig(source.transform))
            return;

        Animator animator = build.Rig.CopyOf(source.transform).GetComponent<Animator>();
        if (animator != null)
            motions.Add(new OverworldAnimator(animator));
    }

    public void Update(GhostFrame frame)
    {
        if (hasOnGround)
            animator.SetBool(onGroundHash, frame.IsGrounded);

        if (!frame.IsGrounded)
            verticalVelocity = frame.Velocity.y;
        if (hasVerticalVelocity)
            animator.SetFloat(verticalVelocityHash, verticalVelocity);

        if (!hasBush)
            return;

        bool hide = frame.IsGrounded && frame.Speed < StandingStillSpeed;
        if (hide == hidden)
            return;

        hidden = hide;
        animator.Play(hide ? hideHash : exitHash, 0);
    }

    private static bool HasParameter(Animator animator, string name)
    {
        var parameters = animator.parameters;
        for (int i = 0; i < parameters.Length; i++)
        {
            if (parameters[i].name == name)
                return true;
        }
        return false;
    }
}
