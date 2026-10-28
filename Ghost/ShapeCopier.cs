using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetterGhosts;

public class ShapeCopier
{
    private readonly GhostPalette palette;
    private readonly GhostPivots pivots;
    private readonly PowerEffects power;
    private readonly List<Mesh> bakedMeshes = new();

    public ShapeCopier(GhostPalette palette, GhostPivots pivots, PowerEffects power)
    {
        this.palette = palette;
        this.pivots = pivots;
        this.power = power;
    }

    public void Copy(GhostBuild build)
    {
        CopyRenderers(build);
        CopyPowerEffects(build);
    }

    public void Destroy()
    {
        foreach (Mesh mesh in bakedMeshes)
            Object.Destroy(mesh);
    }

    private void CopyRenderers(GhostBuild build)
    {
        var renderers = build.Vehicle.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer renderer = renderers[i];
            if (!renderer.enabled || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly || renderer.GetComponent<Billboard>() != null)
                continue;
            if (build.IsInRig(renderer.transform) || renderer.GetComponentInParent<Fire>(true) != null)
                continue;

            SkinnedMeshRenderer skinned = renderer.TryCast<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                CopyRenderer(renderer, Bake(skinned), null);
                continue;
            }

            if (renderer.TryCast<MeshRenderer>() == null)
                continue;
            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            Mesh mesh = filter.sharedMesh;
            CopyRenderer(renderer, mesh, palette.SlotStream(mesh.vertexCount));
        }
    }

    private void CopyPowerEffects(GhostBuild build)
    {
        var fires = build.Vehicle.GetComponentsInChildren<Fire>(true);
        for (int i = 0; i < fires.Length; i++)
        {
            if (fires[i].gameObject.activeInHierarchy && !build.IsInRig(fires[i].transform))
                power.AddDetachedFire(fires[i], pivots);
        }

        var blocks = build.Vehicle.GetComponentsInChildren<JetpackBuildingBlock>(true);
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].gameObject.activeInHierarchy && !build.IsInRig(blocks[i].transform))
                power.AddDetachedBlock(blocks[i], pivots);
        }
    }

    private Mesh Bake(SkinnedMeshRenderer skinned)
    {
        Mesh baked = new Mesh();
        skinned.BakeMesh(baked);
        baked.SetUVs(GhostPalette.SlotChannel, palette.Slots(baked.vertexCount));
        bakedMeshes.Add(baked);
        return baked;
    }

    private void CopyRenderer(Renderer source, Mesh mesh, Mesh paletteSlotStream)
    {
        GameObject copy = new GameObject(source.gameObject.name);
        copy.layer = pivots.Layer;
        pivots.Place(copy.transform, source.transform);

        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
        MeshRenderer renderer = copy.AddComponent<MeshRenderer>();
        if (paletteSlotStream != null)
            renderer.additionalVertexStreams = paletteSlotStream;
        palette.Paint(renderer, mesh.subMeshCount);
    }
}
