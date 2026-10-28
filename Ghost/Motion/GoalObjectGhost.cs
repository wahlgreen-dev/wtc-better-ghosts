using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetterGhosts;

public class GoalObjectGhost : IGhostMotion
{
    private readonly Transform copy;
    private readonly Quaternion rotation;
    private readonly PartRecording recording;
    private readonly int index;

    private GoalObjectGhost(Transform source, int index, PartRecording recording, Transform root, GhostPalette palette, int layer)
    {
        this.index = index;
        this.recording = recording;
        rotation = source.rotation;
        copy = new GameObject("BetterGhost " + source.name).transform;
        copy.gameObject.layer = layer;
        copy.SetParent(root, false);
        copy.SetPositionAndRotation(source.position, rotation);
        copy.localScale = Vector3.Scale(source.lossyScale, Inverse(root.lossyScale));

        var renderers = source.GetComponentsInChildren<MeshRenderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (!renderer.enabled || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            CopyRenderer(renderer, filter.sharedMesh, source, palette, layer);
        }
    }

    public static IEnumerable<IGhostMotion> CreateAll(GhostBuild build, Transform[] goalObjects, PartRecording recording, GhostPalette palette)
    {
        List<IGhostMotion> ghosts = new();
        if (recording == null || !recording.GoalObjectNames.SequenceEqual(GoalObjects.Names(goalObjects)))
            return ghosts;

        for (int i = 0; i < goalObjects.Length; i++)
            ghosts.Add(new GoalObjectGhost(goalObjects[i], i, recording, build.Root, palette, build.Pivots.Layer));
        return ghosts;
    }

    public void Update(GhostFrame frame)
    {
        Vector3 position = Vector3.Lerp(recording.GoalPositions[frame.From][index], recording.GoalPositions[frame.To][index], frame.Blend);
        copy.SetPositionAndRotation(position, rotation);
    }

    private void CopyRenderer(MeshRenderer source, Mesh mesh, Transform anchor, GhostPalette palette, int layer)
    {
        GameObject rendererCopy = new GameObject(source.gameObject.name);
        rendererCopy.layer = layer;
        rendererCopy.transform.SetParent(copy, false);
        RelativePose.Copy(rendererCopy.transform, source.transform, anchor);

        rendererCopy.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = rendererCopy.AddComponent<MeshRenderer>();
        renderer.additionalVertexStreams = palette.SlotStream(mesh.vertexCount);
        palette.Paint(renderer, mesh.subMeshCount);
    }

    private static Vector3 Inverse(Vector3 scale)
    {
        return new Vector3(1f / scale.x, 1f / scale.y, 1f / scale.z);
    }
}
