using UnityEngine;

namespace BetterGhosts;

public class RecordedParts : IGhostMotion
{
    private readonly Transform root;
    private readonly PartRecording recording;
    private readonly Transform[] pivots;

    private RecordedParts(Transform root, PartRecording recording, Transform[] pivots)
    {
        this.root = root;
        this.recording = recording;
        this.pivots = pivots;
    }

    public static RecordedParts Create(GhostBuild build, Transform[] parts, PartRecording recording)
    {
        Transform[] pivots = new Transform[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            pivots[i] = build.Pivots.Create(parts[i]);

        return recording != null ? new RecordedParts(build.Root, recording, pivots) : null;
    }

    public void Update(GhostFrame frame)
    {
        int from = frame.From;
        int to = frame.To;
        float blend = frame.Blend;
        if (recording.HasFrame)
            root.SetPositionAndRotation(Vector3.Lerp(recording.FramePositions[from], recording.FramePositions[to], blend), Quaternion.Slerp(recording.FrameRotations[from], recording.FrameRotations[to], blend));

        for (int i = 0; i < pivots.Length; i++)
        {
            if (!recording.PartsRotateOnly)
                pivots[i].localPosition = Vector3.Lerp(recording.Positions[from][i], recording.Positions[to][i], blend);
            pivots[i].localRotation = Quaternion.Slerp(recording.Rotations[from][i], recording.Rotations[to][i], blend);
        }
    }
}
