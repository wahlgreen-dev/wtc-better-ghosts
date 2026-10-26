using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using Il2CppSpeed.GhostCar;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetterGhosts;

public class ShapedGhost
{
    const string GenericModelName = "GhostCarModel";
    const int PaletteSlotChannel = 3;
    const float GhostBodyPaletteSlot = 34.09375f;

    readonly GameObject root;
    readonly Material material;
    readonly GameObject genericModel;
    readonly Transform[] partPivots;
    readonly PartRecording parts;
    readonly List<float> bodyTimes = new();
    readonly List<Vector3> bodyPositions = new();
    readonly List<Quaternion> bodyRotations = new();
    readonly SampleCursor bodyCursor;
    readonly List<Mesh> createdMeshes = new();
    readonly Dictionary<int, Mesh> paletteSlotStreams = new();

    public ShapedGhost(GhostCarManager manager, Transform vehicle, Transform body, Transform[] parts,
        PartRecording recording)
    {
        var ghost = manager.ghostTransform;
        var modelTransform = TransformPaths.FindDescendant(ghost, GenericModelName);
        var ghostRenderer = FindGhostRenderer(modelTransform != null ? modelTransform : ghost);
        if (ghostRenderer == null)
            throw new System.InvalidOperationException("the game's ghost has no mesh renderer to take the material from");
        material = ghostRenderer.sharedMaterial;

        CacheBodySamples(manager.existingGhostData);
        bodyCursor = new SampleCursor(bodyTimes);
        if (recording != null && recording.SampleCount == bodyTimes.Count)
            this.parts = recording;

        root = new GameObject("BetterGhost");
        root.layer = ghostRenderer.gameObject.layer;
        root.transform.SetParent(modelTransform != null ? modelTransform.parent : ghost, false);
        root.transform.localScale = body.lossyScale;

        partPivots = new Transform[parts.Length];
        for (int i = 0; i < parts.Length; i++)
            partPivots[i] = CreatePivot(parts[i], body, root.transform, ghostRenderer.gameObject.layer);

        CopyRenderers(vehicle, body, parts, ghostRenderer);

        if (modelTransform != null)
        {
            genericModel = modelTransform.gameObject;
            genericModel.SetActive(false);
        }
    }

    public void Update(float time)
    {
        if (root == null)
            return;

        if (!bodyCursor.Seek(time, out var from, out var to, out var blend))
            return;

        root.transform.position = Vector3.Lerp(bodyPositions[from], bodyPositions[to], blend);
        root.transform.rotation = Quaternion.Slerp(bodyRotations[from], bodyRotations[to], blend);

        if (parts != null)
        {
            for (int i = 0; i < partPivots.Length; i++)
            {
                partPivots[i].localPosition = Vector3.Lerp(parts.Positions[from][i], parts.Positions[to][i], blend);
                partPivots[i].localRotation = Quaternion.Slerp(parts.Rotations[from][i], parts.Rotations[to][i], blend);
            }
        }
    }

    public void Destroy()
    {
        if (genericModel != null)
            genericModel.SetActive(true);
        if (root != null)
            Object.Destroy(root);
        foreach (var mesh in createdMeshes)
        {
            if (mesh != null)
                Object.Destroy(mesh);
        }
    }

    void CacheBodySamples(GhostCarData data)
    {
        if (data == null || data.ghostData == null)
            return;

        var samples = data.ghostData;
        for (int i = 0; i < samples.Count; i++)
        {
            var sample = samples[i];
            bodyTimes.Add(sample.time);
            bodyPositions.Add(sample.position);
            bodyRotations.Add(sample.rotation);
        }
    }

    static Transform CreatePivot(Transform part, Transform body, Transform parent, int layer)
    {
        var pivot = new GameObject(part.name).transform;
        pivot.gameObject.layer = layer;
        pivot.SetParent(parent, false);
        pivot.localPosition = body.InverseTransformPoint(part.position);
        pivot.localRotation = Quaternion.Inverse(body.rotation) * part.rotation;
        pivot.localScale = Divide(part.lossyScale, body.lossyScale);
        return pivot;
    }

    void CopyRenderers(Transform vehicle, Transform body, Transform[] parts, Renderer ghostRenderer)
    {
        var renderers = vehicle.GetComponentsInChildren<Renderer>(false);
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (!renderer.enabled || TransformPaths.HasComponentNamed(renderer.transform, "Billboard"))
                continue;

            var anchorIndex = FindAnchor(renderer.transform, body, parts);
            var anchor = anchorIndex >= 0 ? parts[anchorIndex] : body;
            var parent = anchorIndex >= 0 ? partPivots[anchorIndex] : root.transform;

            var skinned = renderer.TryCast<SkinnedMeshRenderer>();
            if (skinned != null)
            {
                CopyRenderer(renderer, BakeWithGhostPalette(skinned), null, anchor, parent, ghostRenderer);
                continue;
            }

            if (renderer.TryCast<MeshRenderer>() == null)
                continue;
            var filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null)
                continue;

            var mesh = filter.sharedMesh;
            CopyRenderer(renderer, mesh, PaletteSlotStream(mesh.vertexCount), anchor, parent, ghostRenderer);
        }
    }

    Mesh BakeWithGhostPalette(SkinnedMeshRenderer skinned)
    {
        var baked = new Mesh();
        skinned.BakeMesh(baked);
        baked.SetUVs(PaletteSlotChannel, PaletteSlots(baked.vertexCount));
        createdMeshes.Add(baked);
        return baked;
    }

    Mesh PaletteSlotStream(int vertexCount)
    {
        if (paletteSlotStreams.TryGetValue(vertexCount, out var stream))
            return stream;

        stream = new Mesh();
        var layout = new Il2CppStructArray<VertexAttributeDescriptor>(1);
        layout[0] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 2, 0);
        stream.SetVertexBufferParams(vertexCount, layout);
        stream.SetUVs(PaletteSlotChannel, PaletteSlots(vertexCount));

        paletteSlotStreams[vertexCount] = stream;
        createdMeshes.Add(stream);
        return stream;
    }

    static Il2CppSystem.Collections.Generic.List<Vector2> PaletteSlots(int vertexCount)
    {
        var slots = new Il2CppSystem.Collections.Generic.List<Vector2>(vertexCount);
        var slot = new Vector2(GhostBodyPaletteSlot, 0f);
        for (int i = 0; i < vertexCount; i++)
            slots.Add(slot);
        return slots;
    }

    static int FindAnchor(Transform transform, Transform body, Transform[] parts)
    {
        for (var t = transform; t != null && t != body; t = t.parent)
        {
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i] == t)
                    return i;
            }
        }
        return -1;
    }

    void CopyRenderer(Renderer source, Mesh mesh, Mesh paletteSlotStream, Transform anchor, Transform parent,
        Renderer ghostRenderer)
    {
        var copy = new GameObject(source.gameObject.name);
        copy.layer = ghostRenderer.gameObject.layer;
        var transform = copy.transform;
        transform.SetParent(parent, false);
        transform.localPosition = anchor.InverseTransformPoint(source.transform.position);
        transform.localRotation = Quaternion.Inverse(anchor.rotation) * source.transform.rotation;
        transform.localScale = Divide(source.transform.lossyScale, anchor.lossyScale);

        copy.AddComponent<MeshFilter>().sharedMesh = mesh;
        var renderer = copy.AddComponent<MeshRenderer>();
        renderer.shadowCastingMode = ghostRenderer.shadowCastingMode;
        renderer.receiveShadows = ghostRenderer.receiveShadows;
        if (paletteSlotStream != null)
            renderer.additionalVertexStreams = paletteSlotStream;

        var subMeshCount = mesh.subMeshCount;
        if (subMeshCount <= 1)
        {
            renderer.sharedMaterial = material;
            return;
        }

        var materials = new Il2CppReferenceArray<Material>(subMeshCount);
        for (int i = 0; i < subMeshCount; i++)
            materials[i] = material;
        renderer.sharedMaterials = materials;
    }

    static Renderer FindGhostRenderer(Transform model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>(true);
        Renderer fallback = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            var renderer = renderers[i];
            if (renderer.TryCast<MeshRenderer>() == null && renderer.TryCast<SkinnedMeshRenderer>() == null)
                continue;
            if (renderer.gameObject.name == "Body")
                return renderer;
            fallback ??= renderer;
        }
        return fallback;
    }

    static Vector3 Divide(Vector3 a, Vector3 b)
    {
        return new Vector3(a.x / b.x, a.y / b.y, a.z / b.z);
    }
}
