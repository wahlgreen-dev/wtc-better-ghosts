using System.Collections.Generic;
using UnityEngine;

namespace BetterGhosts;

public class SwingingPivot
{
    private readonly Transform container;
    private readonly Vector3 restOffset;
    private readonly List<Vector3> pivotPositions;
    private readonly List<Quaternion> pivotRotations;
    private readonly List<bool> flipped = new();

    public SwingingPivot(Transform container, Transform source, Animator copiedAnimator, List<float> times, List<Vector3> positions, List<Quaternion> rotations, List<Vector3> velocities)
    {
        this.container = container;
        restOffset = container.localPosition;
        pivotPositions = positions;
        pivotRotations = rotations;

        float swapDistance = source.parent.TransformVector(source.localPosition).magnitude;
        bool isFlipped = false;
        flipped.Add(isFlipped);
        for (int i = 1; i < positions.Count; i++)
        {
            Vector3 expected = positions[i - 1] + velocities[i - 1] * (times[i] - times[i - 1]);
            if (Vector3.Distance(positions[i], expected) > swapDistance)
                isFlipped = !isFlipped;
            flipped.Add(isFlipped);
        }

        SyncAnimator(source.GetComponent<Animator>(), copiedAnimator);
    }

    public void Update(Transform root, int from, int to, float blend)
    {
        Vector3 offset = flipped[blend < 0.5f ? from : to] ? -restOffset : restOffset;
        container.localPosition = offset;
        if (flipped[from] == flipped[to])
            return;

        Vector3 scale = root.lossyScale;
        Vector3 fromCenter = ContainerCenter(from, scale);
        Vector3 toCenter = ContainerCenter(to, scale);
        root.position = Vector3.Lerp(fromCenter, toCenter, blend) - root.rotation * Vector3.Scale(scale, offset);
    }

    private Vector3 ContainerCenter(int sample, Vector3 scale)
    {
        Vector3 offset = flipped[sample] ? -restOffset : restOffset;
        return pivotPositions[sample] + pivotRotations[sample] * Vector3.Scale(scale, offset);
    }

    private static void SyncAnimator(Animator source, Animator copy)
    {
        if (source == null || copy == null)
            return;

        AnimatorStateInfo state = source.GetCurrentAnimatorStateInfo(0);
        int stateHash = state.fullPathHash;
        float normalizedTime = state.normalizedTime;
        copy.Play(stateHash, 0, normalizedTime);
    }
}
