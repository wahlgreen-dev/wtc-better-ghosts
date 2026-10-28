using UnityEngine;

namespace BetterGhosts;

public struct GhostFrame
{
    public float Time;
    public float DeltaTime;
    public int From;
    public int To;
    public float Blend;
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Velocity;
    public bool IsGrounded;
    public float Skid;
    public float Steering;
    public float TurnRate;
    public bool ActionPressed;

    public float Speed => Velocity.magnitude;

    public Vector3 Forward => Rotation * Vector3.forward;
}
