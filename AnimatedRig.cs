using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace BetterGhosts;

public class AnimatedRig
{
    private static readonly HashSet<string> keptComponents = new() { "Transform", "Animator", "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer" };

    private class BakedSkin
    {
        public SkinnedMeshRenderer Skin;
        public Mesh Mesh;
        public Il2CppSystem.Collections.Generic.List<Vector2> PaletteSlots;
    }

    private readonly List<BakedSkin> skins = new();

    private readonly PowerEffects power;

    public Animator Animator { get; }

    public AnimatedRig(Animator source, Transform body, Transform parent, GhostPalette palette, PowerEffects power)
    {
        this.power = power;
        GameObject holder = new GameObject("BetterGhostRigHolder");
        holder.SetActive(false);
        GameObject copy = Object.Instantiate(source.gameObject, holder.transform);
        copy.name = source.gameObject.name;

        power.AddFromCopy(copy, source.gameObject);
        RemoveBillboards(copy.transform);
        ComponentStripper.Strip(copy, keptComponents, power.Keeps);
        ComponentStripper.SetLayer(copy.transform, parent.gameObject.layer);

        copy.transform.SetParent(parent, false);
        RelativePose.Copy(copy.transform, source.transform, body);
        Object.Destroy(holder);

        PaintMeshRenderers(copy, palette);
        PrepareSkins(copy, palette);

        Animator = copy.GetComponent<Animator>();
        Animator.applyRootMotion = false;
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    public void BakeSkins()
    {
        foreach (BakedSkin baked in skins)
        {
            baked.Skin.BakeMesh(baked.Mesh);
            baked.Mesh.SetUVs(GhostPalette.SlotChannel, baked.PaletteSlots);
        }
    }

    public void Destroy()
    {
        foreach (BakedSkin baked in skins)
            Object.Destroy(baked.Mesh);
    }

    private void RemoveBillboards(Transform root)
    {
        Il2CppArrayBase<Renderer> renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Transform transform = renderers[i].transform;
            if (!power.Keeps(transform) && TransformPaths.HasComponentNamed(transform, "Billboard"))
                Object.DestroyImmediate(renderers[i]);
        }
    }

    private void PaintMeshRenderers(GameObject root, GhostPalette palette)
    {
        Il2CppArrayBase<MeshRenderer> renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (power.Keeps(renderer.transform))
                continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                renderer.enabled = false;
                continue;
            }

            palette.Paint(renderer, filter.sharedMesh.subMeshCount);
            renderer.additionalVertexStreams = palette.SlotStream(filter.sharedMesh.vertexCount);
        }
    }

    private void PrepareSkins(GameObject root, GhostPalette palette)
    {
        Il2CppArrayBase<SkinnedMeshRenderer> skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer skin = skinnedRenderers[i];
            bool wasEnabled = skin.enabled;
            skin.enabled = false;
            if (!wasEnabled)
                continue;

            GameObject bakedObject = new GameObject(skin.gameObject.name + " (baked)");
            bakedObject.layer = skin.gameObject.layer;
            bakedObject.transform.SetParent(skin.transform, false);

            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            bakedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            palette.Paint(bakedObject.AddComponent<MeshRenderer>(), skin.sharedMesh.subMeshCount);

            skins.Add(new BakedSkin { Skin = skin, Mesh = mesh, PaletteSlots = GhostPalette.Slots(skin.sharedMesh.vertexCount) });
        }
        BakeSkins();
    }
}
