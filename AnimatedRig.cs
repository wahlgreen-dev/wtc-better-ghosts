using System.Collections.Generic;
using UnityEngine;

namespace BetterGhosts;

public class AnimatedRig
{
    const int MaxStripPasses = 5;

    static readonly HashSet<string> keptComponents = new()
    {
        "Transform", "Animator", "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer",
    };

    class BakedSkin
    {
        public SkinnedMeshRenderer Skin;
        public Mesh Mesh;
        public Il2CppSystem.Collections.Generic.List<Vector2> PaletteSlots;
    }

    readonly List<BakedSkin> skins = new();

    public Animator Animator { get; }

    public AnimatedRig(Animator source, Transform body, Transform parent, GhostPalette palette)
    {
        var holder = new GameObject("BetterGhostRigHolder");
        holder.SetActive(false);
        var copy = Object.Instantiate(source.gameObject, holder.transform);
        copy.name = source.gameObject.name;

        RemoveBillboardRenderers(copy.transform);
        StripComponents(copy);
        SetLayer(copy.transform, parent.gameObject.layer);

        var transform = copy.transform;
        transform.SetParent(parent, false);
        transform.localPosition = body.InverseTransformPoint(source.transform.position);
        transform.localRotation = Quaternion.Inverse(body.rotation) * source.transform.rotation;
        transform.localScale = new Vector3(
            source.transform.lossyScale.x / body.lossyScale.x,
            source.transform.lossyScale.y / body.lossyScale.y,
            source.transform.lossyScale.z / body.lossyScale.z);
        Object.Destroy(holder);

        PaintMeshRenderers(copy, palette);
        PrepareSkins(copy, palette);

        Animator = copy.GetComponent<Animator>();
        Animator.applyRootMotion = false;
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
    }

    public void BakeSkins()
    {
        foreach (var baked in skins)
        {
            if (baked.Skin == null)
                continue;
            baked.Skin.BakeMesh(baked.Mesh);
            baked.Mesh.SetUVs(GhostPalette.SlotChannel, baked.PaletteSlots);
        }
    }

    public void Destroy()
    {
        foreach (var baked in skins)
        {
            if (baked.Mesh != null)
                Object.Destroy(baked.Mesh);
        }
    }

    static void RemoveBillboardRenderers(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (TransformPaths.HasComponentNamed(renderers[i].transform, "Billboard"))
                Object.DestroyImmediate(renderers[i]);
        }
    }

    static void StripComponents(GameObject root)
    {
        for (int pass = 0; pass < MaxStripPasses; pass++)
        {
            var removed = false;
            var components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                var component = components[i];
                if (component == null || keptComponents.Contains(component.GetIl2CppType().Name))
                    continue;
                if (!CanRemoveThisPass(component, pass))
                    continue;
                Object.DestroyImmediate(component);
                removed = true;
            }
            if (!removed && pass > 2)
                return;
        }
    }

    static bool CanRemoveThisPass(Component component, int pass)
    {
        if (component.TryCast<MonoBehaviour>() != null || component.TryCast<Joint>() != null)
            return true;
        if (component.TryCast<Rigidbody>() != null)
            return pass >= 2;
        return pass >= 1;
    }

    static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayer(root.GetChild(i), layer);
    }

    static void PaintMeshRenderers(GameObject root, GhostPalette palette)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
            {
                renderer.enabled = false;
                continue;
            }
            palette.Paint(renderer, filter.sharedMesh.subMeshCount);
            renderer.additionalVertexStreams = palette.SlotStream(filter.sharedMesh.vertexCount);
        }
    }

    void PrepareSkins(GameObject root, GhostPalette palette)
    {
        var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            var skin = skinnedRenderers[i];
            var sourceMesh = skin.sharedMesh;
            var wasEnabled = skin.enabled;
            skin.enabled = false;
            if (sourceMesh == null || !wasEnabled)
                continue;

            var bakedObject = new GameObject(skin.gameObject.name + " (baked)");
            bakedObject.layer = skin.gameObject.layer;
            bakedObject.transform.SetParent(skin.transform, false);

            var mesh = new Mesh();
            mesh.MarkDynamic();
            bakedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = bakedObject.AddComponent<MeshRenderer>();
            palette.Paint(renderer, sourceMesh.subMeshCount);

            var baked = new BakedSkin { Skin = skin, Mesh = mesh, PaletteSlots = GhostPalette.Slots(sourceMesh.vertexCount) };
            skins.Add(baked);
        }
        BakeSkins();
    }
}
