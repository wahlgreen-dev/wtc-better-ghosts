using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class AnimatorDriver : IGhostMotion
{
    private const float GroundRayLift = 0.5f;
    private const float GroundRayLength = 50f;
    private const string PumpParamName = "Pump";
    private const string HorizontalParamName = "Horizontal";
    private const string PedalParamName = "Velocity";

    private class VelocityTransfer
    {
        public int Hash;
        public Vector3 Axis;
        public float Factor;
        public bool LocalSpace;
        public bool Clamp;
        public float ClampMagnitude;
        public float SmoothTime;
        public float Current;
        public float SmoothVelocity;
    }

    private readonly Animator sourceAnimator;
    private readonly Animator animator;
    private readonly List<VelocityTransfer> transfers = new();
    private readonly List<(int hash, float value)> fixedFloats = new();
    private readonly List<(int hash, bool value)> fixedBools = new();
    private readonly int groundDistanceHash;
    private readonly int isGroundedHash;
    private readonly int groundMask = Physics.DefaultRaycastLayers;
    private readonly int legSpeedHash;
    private readonly int legSpeedMultiplierHash;
    private readonly int skidHash;
    private readonly int landedHash;
    private readonly int pumpHash;
    private readonly int horizontalHash;
    private readonly int pedalHash;
    private readonly float maxPedalSpeed;
    private readonly float pumpInterval;
    private float pumpCooldown;
    private float groundClearance;
    private bool wasGrounded = true;

    private AnimatorDriver(Transform vehicle, Animator sourceAnimator, Animator animator)
    {
        this.sourceAnimator = sourceAnimator;
        this.animator = animator;

        var transferComponents = vehicle.GetComponentsInChildren<TransferVelocityToAnimationProperty>(true);
        for (int i = 0; i < transferComponents.Length; i++)
        {
            TransferVelocityToAnimationProperty component = transferComponents[i];
            if (component.animator != sourceAnimator || component.onlyWhenBoosting || component.addToPreviousValue)
                continue;

            transfers.Add(new VelocityTransfer
            {
                Hash = Animator.StringToHash(component.propertyName),
                Axis = component.axis,
                Factor = component.velocityToAnimSpeedFactor,
                LocalSpace = component.convertToLocalSpace,
                Clamp = component.clamp,
                ClampMagnitude = component.clampMagnitude,
                SmoothTime = component.smoothTime,
            });
        }

        GroundDistanceAnimationParameter ground = vehicle.GetComponentInChildren<GroundDistanceAnimationParameter>(true);
        if (ground != null && ground.animator == sourceAnimator)
        {
            groundDistanceHash = Animator.StringToHash(ground.groundDistanceParamName);
            isGroundedHash = Animator.StringToHash(ground.isGroundedParamName);
            int mask = ground.groundMask.value;
            if (mask != 0)
                groundMask = mask;
        }

        var setters = vehicle.GetComponentsInChildren<AnimatorParamSetter>(true);
        for (int i = 0; i < setters.Length; i++)
        {
            AnimatorParamSetter setter = setters[i];
            if (setter.anim != sourceAnimator)
                continue;

            int hash = Animator.StringToHash(setter.paramName);
            if (setter.autoRunSetFloat)
                fixedFloats.Add((hash, setter.floatValue));
            if (setter.autoRunSetBool)
                fixedBools.Add((hash, setter.boolValue));
        }

        CarWithLegsSkater skater = vehicle.GetComponentInChildren<CarWithLegsSkater>(true);
        if (skater != null && skater.animator == sourceAnimator)
        {
            pumpHash = Animator.StringToHash(PumpParamName);
            horizontalHash = Animator.StringToHash(HorizontalParamName);
            pumpInterval = skater.pumpDuration;
        }

        CarOnBikeController bike = vehicle.GetComponentInChildren<CarOnBikeController>(true);
        if (bike != null && bike.animator == sourceAnimator)
        {
            pedalHash = Animator.StringToHash(PedalParamName);
            maxPedalSpeed = bike.maxPedalAnimationSpeed;
        }

        CarWithLegs legs = vehicle.GetComponentInChildren<CarWithLegs>(true);
        if (legs == null || legs.animator != sourceAnimator)
            return;

        legSpeedHash = Animator.StringToHash(legs.legSpeedParamName);
        legSpeedMultiplierHash = Animator.StringToHash(legs.legSpeedMultiplierParamName);
        skidHash = Animator.StringToHash(legs.skidParamName);
        landedHash = legs.landedHash;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        if (build.Setup.RigRoot == null)
            return;

        Animator source = build.Setup.RigRoot.GetComponentInChildren<Animator>(true);
        if (source == null)
            return;

        Transform copy = build.MovingPartFor(source.transform);
        Animator animator = copy != null ? copy.GetComponent<Animator>() : null;
        if (animator != null)
            motions.Add(new AnimatorDriver(build.Vehicle, source, animator));
    }

    public void Update(GhostFrame frame)
    {
        foreach ((int hash, float value) in fixedFloats)
            animator.SetFloat(hash, value);
        foreach ((int hash, bool value) in fixedBools)
            animator.SetBool(hash, value);

        foreach (VelocityTransfer transfer in transfers)
            animator.SetFloat(transfer.Hash, TransferValue(transfer, frame));

        if (legSpeedHash != 0)
        {
            animator.SetFloat(legSpeedHash, sourceAnimator.GetFloat(legSpeedHash));
            animator.SetFloat(legSpeedMultiplierHash, sourceAnimator.GetFloat(legSpeedMultiplierHash));
            animator.SetFloat(skidHash, frame.Skid);
        }

        if (pedalHash != 0)
            animator.SetFloat(pedalHash, frame.IsGrounded ? Mathf.Min(frame.Velocity.magnitude, maxPedalSpeed) : 0f);

        if (pumpHash != 0)
            DrivePump(frame);

        if (frame.IsGrounded && !wasGrounded && landedHash != 0)
            animator.SetTrigger(landedHash);
        wasGrounded = frame.IsGrounded;

        if (groundDistanceHash == 0)
            return;

        animator.SetFloat(groundDistanceHash, GroundDistance(frame));
        animator.SetBool(isGroundedHash, frame.IsGrounded);
    }

    private void DrivePump(GhostFrame frame)
    {
        animator.SetFloat(horizontalHash, frame.Steering);
        pumpCooldown -= frame.DeltaTime;
        if (!frame.ActionPressed || pumpCooldown > 0f)
            return;

        animator.SetTrigger(pumpHash);
        pumpCooldown = pumpInterval;
    }

    private float TransferValue(VelocityTransfer transfer, GhostFrame frame)
    {
        Vector3 velocity = transfer.LocalSpace ? Quaternion.Inverse(frame.Rotation) * frame.Velocity : frame.Velocity;
        bool verticalOnly = transfer.Axis.x == 0f && transfer.Axis.z == 0f;
        float target = verticalOnly ? Vector3.Dot(velocity, transfer.Axis) : Vector3.Scale(velocity, transfer.Axis).magnitude;
        target *= transfer.Factor;
        if (transfer.Clamp)
            target = Mathf.Clamp(target, -transfer.ClampMagnitude, transfer.ClampMagnitude);

        if (transfer.SmoothTime <= 0f || frame.DeltaTime <= 0f)
        {
            transfer.Current = target;
            return target;
        }

        transfer.Current = Mathf.SmoothDamp(transfer.Current, target, ref transfer.SmoothVelocity, transfer.SmoothTime, Mathf.Infinity, frame.DeltaTime);
        return transfer.Current;
    }

    private float GroundDistance(GhostFrame frame)
    {
        Vector3 origin = frame.Position + Vector3.up * GroundRayLift;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundRayLength, groundMask, QueryTriggerInteraction.Ignore))
            return GroundRayLength;

        float height = hit.distance - GroundRayLift;
        if (!frame.IsGrounded)
            return Mathf.Max(0f, height - groundClearance);

        groundClearance = height;
        return 0f;
    }
}
