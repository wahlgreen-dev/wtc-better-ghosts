using System.Collections.Generic;
using UnityEngine;

namespace BetterGhosts;

public class GhostPivots
{
    private readonly Transform root;
    private readonly Transform body;
    private readonly List<Transform> sources = new();
    private readonly List<Transform> pivots = new();

    public GhostPivots(Transform root, Transform body, int layer)
    {
        this.root = root;
        this.body = body;
        Layer = layer;
    }

    public int Layer { get; }

    public Transform Create(Transform source)
    {
        Transform pivot = new GameObject(source.name).transform;
        pivot.gameObject.layer = Layer;
        Place(pivot, source);
        sources.Add(source);
        pivots.Add(pivot);
        return pivot;
    }

    public void Place(Transform copy, Transform source)
    {
        int index = NearestPivotIndex(source);
        Transform anchor = index >= 0 ? sources[index] : body;
        copy.SetParent(index >= 0 ? pivots[index] : root, false);
        RelativePose.Copy(copy, source, anchor);
    }

    public Transform ParentFor(Transform source)
    {
        int index = NearestPivotIndex(source);
        return index >= 0 ? pivots[index] : root;
    }

    private int NearestPivotIndex(Transform source)
    {
        for (Transform t = source; t != null && t != body; t = t.parent)
        {
            for (int i = 0; i < sources.Count; i++)
            {
                if (sources[i] == t)
                    return i;
            }
        }
        return -1;
    }
}
