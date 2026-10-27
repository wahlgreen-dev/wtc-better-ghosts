using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class InferredWheel
{
    private const float SpeedVariation = 0.1f;
    private const float DriftAmount = 0.1f;
    private const float DriftRate = 0.3f;
    private const float MovingSpeed = 1f;
    private const float SpinUpTime = 0.5f;

    private readonly Transform pivot;
    private readonly Vector3 axis;
    private readonly Vector3 anchor;
    private readonly Vector3 restPosition;
    private readonly Quaternion restRotation;
    private readonly float speed;
    private readonly float driftSeed;
    private float currentSpeed;
    private float angle;

    public InferredWheel(Transform pivot, Transform wheel, Transform body)
    {
        this.pivot = pivot;
        HingeJoint hinge = wheel.GetComponent<HingeJoint>();
        Quaternion bodyRotationInverse = Quaternion.Inverse(body.rotation);
        axis = (bodyRotationInverse * (wheel.rotation * hinge.axis)).normalized;
        anchor = body.InverseTransformPoint(wheel.TransformPoint(hinge.anchor));
        restPosition = body.InverseTransformPoint(wheel.position);
        restRotation = bodyRotationInverse * wheel.rotation;
        speed = wheel.GetComponent<InputWheel>().maxAngularVelocity * Random.Range(1f - SpeedVariation, 1f + SpeedVariation);
        driftSeed = Random.Range(0f, 100f);
        angle = Random.Range(0f, 360f);
    }

    public void Update(float time, float deltaTime, float ghostSpeed)
    {
        float drift = 1f + DriftAmount * (Mathf.PerlinNoise(driftSeed, time * DriftRate) * 2f - 1f);
        float targetSpeed = ghostSpeed > MovingSpeed ? speed * drift : 0f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, Mathf.Abs(speed) / SpinUpTime * deltaTime);
        angle += currentSpeed * deltaTime;

        Quaternion turn = Quaternion.AngleAxis(angle, axis);
        pivot.localRotation = turn * restRotation;
        pivot.localPosition = anchor + turn * (restPosition - anchor);
    }
}
