using UnityEngine;
using UnityEngine.Rendering;

namespace BetterGhosts;

public class GoalObjectGhost
{
    private readonly GameObject root;

    public GoalObjectGhost(Transform source, GhostPalette palette, int layer)
    {
        root = new GameObject("BetterGhost " + source.name);
        root.layer = layer;
        root.transform.SetPositionAndRotation(source.position, source.rotation);
        root.transform.localScale = source.lossyScale;

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

    public void Update(Vector3 position, bool visible)
    {
        if (root.activeSelf != visible)
            root.SetActive(visible);
        root.transform.position = position;
    }

    public void Destroy()
    {
        Object.Destroy(root);
    }

    private void CopyRenderer(MeshRenderer source, Mesh mesh, Transform anchor, GhostPalette palette, int layer)
    {
        GameObject copy = new GameObject(source.gameObject.name);
        copy.layer = layer;
        copy.transform.SetParent(root.transform, false);
        RelativePose.Copy(copy.transform, source.transform, anchor);

        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = copy.AddComponent<MeshRenderer>();
        renderer.additionalVertexStreams = palette.SlotStream(mesh.vertexCount);
        palette.Paint(renderer, mesh.subMeshCount);
    }
}
