using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class LeaningPart
{
    private readonly Transform pivot;
    private readonly Quaternion restRotation;
    private readonly float maxAngle;
    private readonly AnimationCurve leanToAngle;
    private readonly float rotateSpeed;
    private readonly float direction;
    private readonly bool onlyIfGrounded;
    private readonly float maxInputContribution;
    private float angle;

    public LeaningPart(Transform pivot, LeanWithSkid source)
    {
        this.pivot = pivot;
        restRotation = pivot.localRotation;
        maxAngle = source.maxAngle;
        leanToAngle = source.leanToAngle;
        rotateSpeed = source.rotateSpeed;
        direction = source.invert ? -1f : 1f;
        onlyIfGrounded = source.onlyIfGrounded;
        maxInputContribution = source.maxInputContribution;
    }

    public void Update(float skid, float steering, bool grounded, float deltaTime)
    {
        float lean = Mathf.Clamp(skid + steering * maxInputContribution, -1f, 1f);
        float target = onlyIfGrounded && !grounded ? 0f : Mathf.Sign(lean) * leanToAngle.Evaluate(Mathf.Abs(lean)) * maxAngle * direction;
        angle = Mathf.MoveTowards(angle, target, rotateSpeed * deltaTime);
        pivot.localRotation = restRotation * Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
