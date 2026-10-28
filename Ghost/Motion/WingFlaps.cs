using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class WingFlaps : IGhostMotion
{
    private readonly Animator wings;
    private readonly Animator bike;
    private readonly int openHash;
    private readonly int airborneHash;
    private readonly int diveHash;
    private bool openedThisFlight;

    private WingFlaps(Animator wings, Animator bike)
    {
        this.wings = wings;
        this.bike = bike;
        openHash = Animator.StringToHash(WingedGlider.wingOpenParamName);
        airborneHash = Animator.StringToHash(WingedGlider.airborneParamName);
        diveHash = Animator.StringToHash(WingedGlider.diveParamName);
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        WingedGlider source = build.Vehicle.GetComponentInChildren<WingedGlider>(true);
        if (source == null || source.wingAnimator == null || build.Rig == null || !build.IsInRig(source.wingAnimator.transform))
            return;

        Animator wings = build.Rig.CopyOf(source.wingAnimator.transform).GetComponent<Animator>();
        Animator bike = source.bikeAnimator != null && build.IsInRig(source.bikeAnimator.transform) ? build.Rig.CopyOf(source.bikeAnimator.transform).GetComponent<Animator>() : null;
        if (wings != null)
            motions.Add(new WingFlaps(wings, bike));
    }

    public void Update(GhostFrame frame)
    {
        bool airborne = !frame.IsGrounded;
        bool open = airborne && frame.ActionPressed;
        if (!airborne)
            openedThisFlight = false;
        else if (open)
            openedThisFlight = true;

        wings.SetBool(airborneHash, airborne);
        wings.SetBool(openHash, open);
        if (bike != null)
            bike.SetBool(diveHash, airborne && !open && openedThisFlight);
    }
}
