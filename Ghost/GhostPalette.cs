using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Rendering;
using Vector2List = Il2CppSystem.Collections.Generic.List<UnityEngine.Vector2>;

namespace BetterGhosts;

public class GhostPalette
{
    public const int SlotChannel = 3;
    private const float GhostBodySlot = 34.09375f;
    private const float GhostBodyPaletteEntry = 34f;
    private const string PaletteOffsetProperty = "_ColorPaletteOffset";

    private static readonly Dictionary<int, Vector2List> slotLists = new();

    private readonly Material material;
    private readonly ShadowCastingMode shadowCastingMode;
    private readonly bool receiveShadows;
    private readonly Dictionary<int, Mesh> slotStreams = new();
    private Material offsetMaterial;

    public GhostPalette(Renderer ghostRenderer)
    {
        material = ghostRenderer.sharedMaterial;
        shadowCastingMode = ghostRenderer.shadowCastingMode;
        receiveShadows = ghostRenderer.receiveShadows;
    }

    public void Paint(Renderer renderer, int subMeshCount)
    {
        Paint(renderer, subMeshCount, material);
    }

    public void PaintWithoutSlots(Renderer renderer, int subMeshCount)
    {
        if (offsetMaterial == null)
        {
            offsetMaterial = new Material(material);
            offsetMaterial.SetFloat(PaletteOffsetProperty, GhostBodyPaletteEntry);
        }
        Paint(renderer, subMeshCount, offsetMaterial);
    }

    private void Paint(Renderer renderer, int subMeshCount, Material paint)
    {
        renderer.shadowCastingMode = shadowCastingMode;
        renderer.receiveShadows = receiveShadows;
        if (subMeshCount <= 1)
        {
            renderer.sharedMaterial = paint;
            return;
        }

        Il2CppReferenceArray<Material> materials = new Il2CppReferenceArray<Material>(subMeshCount);
        for (int i = 0; i < subMeshCount; i++)
            materials[i] = paint;
        renderer.sharedMaterials = materials;
    }

    public Mesh SlotStream(int vertexCount)
    {
        if (slotStreams.TryGetValue(vertexCount, out Mesh stream))
            return stream;

        stream = new Mesh();
        Il2CppStructArray<VertexAttributeDescriptor> layout = new Il2CppStructArray<VertexAttributeDescriptor>(1);
        layout[0] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 2, 0);
        stream.SetVertexBufferParams(vertexCount, layout);
        stream.SetUVs(SlotChannel, Slots(vertexCount));
        slotStreams[vertexCount] = stream;
        return stream;
    }

    public Vector2List Slots(int vertexCount)
    {
        if (slotLists.TryGetValue(vertexCount, out Vector2List slots))
            return slots;

        slots = new Vector2List(vertexCount);
        Vector2 slot = new Vector2(GhostBodySlot, 0f);
        for (int i = 0; i < vertexCount; i++)
            slots.Add(slot);
        slotLists[vertexCount] = slots;
        return slots;
    }

    public void Destroy()
    {
        foreach (Mesh stream in slotStreams.Values)
            Object.Destroy(stream);
        if (offsetMaterial != null)
            Object.Destroy(offsetMaterial);
    }
}
