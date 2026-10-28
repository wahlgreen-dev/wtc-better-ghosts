using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class LegKick : IGhostMotion
{
    private static readonly int kickHash = Animator.StringToHash("Kick");
    private static readonly int rechargedHash = Animator.StringToHash("Recharged");

    private readonly Animator animator;
    private bool wasPressed;

    private LegKick(Animator animator)
    {
        this.animator = animator;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        if (build.Rig == null)
            return;

        OneLegKick kick = build.Vehicle.GetComponentInChildren<OneLegKick>(true);
        if (kick == null || kick.animator == null || !build.IsInRig(kick.animator.transform))
            return;

        Animator animator = build.Rig.CopyOf(kick.animator.transform).GetComponent<Animator>();
        if (animator == null)
            return;

        animator.SetBool(rechargedHash, true);
        motions.Add(new LegKick(animator));
    }

    public void Update(GhostFrame frame)
    {
        if (frame.ActionPressed && !wasPressed)
            animator.SetTrigger(kickHash);
        wasPressed = frame.ActionPressed;
    }
}
