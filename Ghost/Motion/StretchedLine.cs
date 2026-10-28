using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetterGhosts;

public class StretchedLine : IGhostMotion
{
    private static Mesh cube;

    private readonly Transform start;
    private readonly Transform end;
    private readonly Transform line;
    private readonly float width;

    private StretchedLine(Transform start, Transform end, Transform line, float width)
    {
        this.start = start;
        this.end = end;
        this.line = line;
        this.width = width;
    }

    public static void Find(GhostBuild build, List<IGhostMotion> motions)
    {
        if (build.Rig != null)
            return;

        var sources = build.Vehicle.GetComponentsInChildren<MovingLineRenderer>(false);
        for (int i = 0; i < sources.Length; i++)
        {
            MovingLineRenderer source = sources[i];
            LineRenderer renderer = source.GetComponent<LineRenderer>();
            if (source.asCatenary || source.lineStart == null || source.lineEnd == null || renderer == null)
                continue;
            if (!renderer.enabled || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                continue;
            if (build.IsInRig(source.lineStart) || build.IsInRig(source.lineEnd))
                continue;

            Transform start = Point(build, source.lineStart);
            Transform end = Point(build, source.lineEnd);
            Transform line = CreateLine(build, source.gameObject.name);
            motions.Add(new StretchedLine(start, end, line, renderer.startWidth));
        }
    }

    public void Update(GhostFrame frame)
    {
        Vector3 from = start.position;
        Vector3 offset = end.position - from;
        float length = offset.magnitude;
        if (length <= 0f)
            return;

        line.SetPositionAndRotation(from + offset * 0.5f, Quaternion.LookRotation(offset / length));
        line.localScale = new Vector3(width, width, length);
    }

    private static Transform Point(GhostBuild build, Transform source)
    {
        Transform point = new GameObject(source.name).transform;
        point.gameObject.layer = build.Pivots.Layer;
        build.Pivots.Place(point, source);
        return point;
    }

    private static Transform CreateLine(GhostBuild build, string name)
    {
        GameObject line = new GameObject(name);
        line.layer = build.Pivots.Layer;
        line.transform.SetParent(build.Root, false);

        Mesh mesh = Cube();
        line.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = line.AddComponent<MeshRenderer>();
        renderer.additionalVertexStreams = build.Palette.SlotStream(mesh.vertexCount);
        build.Palette.Paint(renderer, 1);
        return line.transform;
    }

    private static Mesh Cube()
    {
        if (cube != null)
            return cube;

        GameObject primitive = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube = primitive.GetComponent<MeshFilter>().sharedMesh;
        Object.DestroyImmediate(primitive);
        return cube;
    }
}
