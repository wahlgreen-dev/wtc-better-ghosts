using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public struct GhostMotion
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public bool IsGrounded;
    public float Skid;
}

public class AnimatorDriver
{
    private const float GroundRayLift = 0.5f;
    private const float GroundRayLength = 50f;

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
    private float groundClearance;
    private bool wasGrounded = true;

    public AnimatorDriver(Transform vehicle, Animator sourceAnimator)
    {
        this.sourceAnimator = sourceAnimator;

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

        CarWithLegs legs = vehicle.GetComponentInChildren<CarWithLegs>(true);
        if (legs == null || legs.animator != sourceAnimator)
            return;

        legSpeedHash = Animator.StringToHash(legs.legSpeedParamName);
        legSpeedMultiplierHash = Animator.StringToHash(legs.legSpeedMultiplierParamName);
        skidHash = Animator.StringToHash(legs.skidParamName);
        landedHash = legs.landedHash;
    }

    public void Drive(Animator animator, GhostMotion motion, float deltaTime)
    {
        foreach ((int hash, float value) in fixedFloats)
            animator.SetFloat(hash, value);
        foreach ((int hash, bool value) in fixedBools)
            animator.SetBool(hash, value);

        foreach (VelocityTransfer transfer in transfers)
            animator.SetFloat(transfer.Hash, TransferValue(transfer, motion, deltaTime));

        if (legSpeedHash != 0)
        {
            animator.SetFloat(legSpeedHash, sourceAnimator.GetFloat(legSpeedHash));
            animator.SetFloat(legSpeedMultiplierHash, sourceAnimator.GetFloat(legSpeedMultiplierHash));
            animator.SetFloat(skidHash, motion.Skid);
        }

        if (motion.IsGrounded && !wasGrounded && landedHash != 0)
            animator.SetTrigger(landedHash);
        wasGrounded = motion.IsGrounded;

        if (groundDistanceHash == 0)
            return;

        animator.SetFloat(groundDistanceHash, GroundDistance(motion));
        animator.SetBool(isGroundedHash, motion.IsGrounded);
    }

    private float TransferValue(VelocityTransfer transfer, GhostMotion motion, float deltaTime)
    {
        Vector3 velocity = transfer.LocalSpace ? Quaternion.Inverse(motion.Rotation) * motion.Velocity : motion.Velocity;
        bool verticalOnly = transfer.Axis.x == 0f && transfer.Axis.z == 0f;
        float target = verticalOnly ? Vector3.Dot(velocity, transfer.Axis) : Vector3.Scale(velocity, transfer.Axis).magnitude;
        target *= transfer.Factor;
        if (transfer.Clamp)
            target = Mathf.Clamp(target, -transfer.ClampMagnitude, transfer.ClampMagnitude);

        if (transfer.SmoothTime <= 0f || deltaTime <= 0f)
        {
            transfer.Current = target;
            return target;
        }

        transfer.Current = Mathf.SmoothDamp(transfer.Current, target, ref transfer.SmoothVelocity, transfer.SmoothTime, Mathf.Infinity, deltaTime);
        return transfer.Current;
    }

    private float GroundDistance(GhostMotion motion)
    {
        Vector3 origin = motion.Position + Vector3.up * GroundRayLift;
        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundRayLength, groundMask, QueryTriggerInteraction.Ignore))
            return GroundRayLength;

        float height = hit.distance - GroundRayLift;
        if (!motion.IsGrounded)
            return Mathf.Max(0f, height - groundClearance);

        groundClearance = height;
        return 0f;
    }
}
