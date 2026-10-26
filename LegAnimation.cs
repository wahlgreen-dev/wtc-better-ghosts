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

public class LegAnimation
{
    const float GroundRayLift = 0.5f;
    const float GroundRayLength = 50f;

    class VelocityTransfer
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

    readonly Animator sourceAnimator;
    readonly List<VelocityTransfer> transfers = new();
    readonly Dictionary<int, float> floatValues = new();
    readonly int groundDistanceHash;
    readonly int isGroundedHash;
    readonly int groundMask = Physics.DefaultRaycastLayers;
    readonly int legSpeedHash;
    readonly int legSpeedMultiplierHash;
    readonly int skidHash;
    readonly int landedHash;
    float groundClearance;
    bool wasGrounded = true;

    public LegAnimation(Transform vehicle, Animator sourceAnimator)
    {
        this.sourceAnimator = sourceAnimator;

        var transferComponents = vehicle.GetComponentsInChildren<TransferVelocityToAnimationProperty>(true);
        for (int i = 0; i < transferComponents.Length; i++)
        {
            var component = transferComponents[i];
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

        var ground = vehicle.GetComponentInChildren<GroundDistanceAnimationParameter>(true);
        if (ground != null && ground.animator == sourceAnimator)
        {
            groundDistanceHash = Animator.StringToHash(ground.groundDistanceParamName);
            isGroundedHash = Animator.StringToHash(ground.isGroundedParamName);
            var mask = ground.groundMask.value;
            if (mask != 0)
                groundMask = mask;
        }

        var legs = vehicle.GetComponentInChildren<CarWithLegs>(true);
        if (legs != null)
        {
            legSpeedHash = Animator.StringToHash(legs.legSpeedParamName);
            legSpeedMultiplierHash = Animator.StringToHash(legs.legSpeedMultiplierParamName);
            skidHash = Animator.StringToHash(legs.skidParamName);
            landedHash = legs.landedHash;
        }
    }

    public void Drive(Animator animator, GhostMotion motion, float deltaTime)
    {
        foreach (var transfer in transfers)
            floatValues[transfer.Hash] = TransferValue(transfer, motion, deltaTime);

        if (groundDistanceHash != 0)
            floatValues[groundDistanceHash] = GroundDistance(motion);

        if (legSpeedHash != 0)
        {
            floatValues[legSpeedHash] = sourceAnimator.GetFloat(legSpeedHash);
            floatValues[legSpeedMultiplierHash] = sourceAnimator.GetFloat(legSpeedMultiplierHash);
            floatValues[skidHash] = motion.Skid;
        }

        foreach (var value in floatValues)
            animator.SetFloat(value.Key, value.Value);
        if (isGroundedHash != 0)
            animator.SetBool(isGroundedHash, motion.IsGrounded);
        if (landedHash != 0 && motion.IsGrounded && !wasGrounded)
            animator.SetTrigger(landedHash);
        wasGrounded = motion.IsGrounded;
    }

    float TransferValue(VelocityTransfer transfer, GhostMotion motion, float deltaTime)
    {
        var velocity = transfer.LocalSpace ? Quaternion.Inverse(motion.Rotation) * motion.Velocity : motion.Velocity;
        var target = Vector3.Scale(velocity, transfer.Axis).magnitude * transfer.Factor;
        if (transfer.Axis.x == 0f && transfer.Axis.z == 0f)
            target = Vector3.Dot(velocity, transfer.Axis) * transfer.Factor;
        if (transfer.Clamp)
            target = Mathf.Clamp(target, -transfer.ClampMagnitude, transfer.ClampMagnitude);

        if (transfer.SmoothTime > 0f && deltaTime > 0f)
            transfer.Current = Mathf.SmoothDamp(transfer.Current, target, ref transfer.SmoothVelocity, transfer.SmoothTime,
                Mathf.Infinity, deltaTime);
        else
            transfer.Current = target;
        return transfer.Current;
    }

    float GroundDistance(GhostMotion motion)
    {
        var origin = motion.Position + Vector3.up * GroundRayLift;
        if (!Physics.Raycast(origin, Vector3.down, out var hit, GroundRayLength, groundMask, QueryTriggerInteraction.Ignore))
            return GroundRayLength;

        var height = hit.distance - GroundRayLift;
        if (motion.IsGrounded)
        {
            groundClearance = height;
            return 0f;
        }
        return Mathf.Max(0f, height - groundClearance);
    }
}
