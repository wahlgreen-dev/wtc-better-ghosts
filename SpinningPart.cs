using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class SpinningPart
{
    private readonly Transform transform;
    private readonly Vector3 axis;
    private readonly float speed;
    private readonly Space space;
    private readonly bool speedBasedOnVelocity;

    public SpinningPart(Transform transform, RotateAroundAxis source)
    {
        this.transform = transform;
        axis = source.axis;
        speed = source.speed * source.speedMod;
        space = source.space;
        speedBasedOnVelocity = source.speedBasedOnVelocity;
    }

    public static bool Spins(RotateAroundAxis rotator)
    {
        return rotator.enabled && rotator.gameObject.activeInHierarchy;
    }

    public void Update(float deltaTime, float velocity)
    {
        float currentSpeed = speedBasedOnVelocity ? speed * velocity : speed;
        transform.Rotate(axis * (currentSpeed * deltaTime), space);
    }
}
