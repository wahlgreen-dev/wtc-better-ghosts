using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class SwingingPivot : IGhostMotion
{
    private readonly Transform root;
    private readonly Transform container;
    private readonly Vector3 restOffset;
    private readonly GhostSamples samples;
    private readonly List<bool> flipped = new();

    private SwingingPivot(Transform root, Transform container, Transform source, GhostSamples samples)
    {
        this.root = root;
        this.container = container;
        this.samples = samples;
        restOffset = container.localPosition;

        float swapDistance = source.parent.TransformVector(source.localPosition).magnitude;
        bool isFlipped = false;
        flipped.Add(isFlipped);
        for (int i = 1; i < samples.Count; i++)
        {
            Vector3 expected = samples.Positions[i - 1] + samples.Velocities[i - 1] * (samples.Times[i] - samples.Times[i - 1]);
            if (Vector3.Distance(samples.Positions[i], expected) > swapDistance)
                isFlipped = !isFlipped;
            flipped.Add(isFlipped);
        }

        SyncAnimator(source.GetComponent<Animator>(), container.GetComponent<Animator>());
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        DoubleSwingers swingers = build.Vehicle.GetComponentInChildren<DoubleSwingers>(true);
        if (swingers == null || swingers.childContainer == null)
            return;

        Transform container = build.MovingPartFor(swingers.childContainer);
        if (container != null)
            motions.Add(new SwingingPivot(build.Root, container, swingers.childContainer, build.Samples));
    }

    public void Update(GhostFrame frame)
    {
        Vector3 offset = flipped[frame.Blend < 0.5f ? frame.From : frame.To] ? -restOffset : restOffset;
        container.localPosition = offset;
        if (flipped[frame.From] == flipped[frame.To])
            return;

        Vector3 scale = root.lossyScale;
        Vector3 fromCenter = ContainerCenter(frame.From, scale);
        Vector3 toCenter = ContainerCenter(frame.To, scale);
        root.position = Vector3.Lerp(fromCenter, toCenter, frame.Blend) - root.rotation * Vector3.Scale(scale, offset);
    }

    private Vector3 ContainerCenter(int sample, Vector3 scale)
    {
        Vector3 offset = flipped[sample] ? -restOffset : restOffset;
        return samples.Positions[sample] + samples.Rotations[sample] * Vector3.Scale(scale, offset);
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
