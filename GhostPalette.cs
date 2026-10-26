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

    private readonly Material material;
    private readonly ShadowCastingMode shadowCastingMode;
    private readonly bool receiveShadows;
    private readonly Dictionary<int, Mesh> slotStreams = new();

    public GhostPalette(Renderer ghostRenderer)
    {
        material = ghostRenderer.sharedMaterial;
        shadowCastingMode = ghostRenderer.shadowCastingMode;
        receiveShadows = ghostRenderer.receiveShadows;
    }

    public void Paint(Renderer renderer, int subMeshCount)
    {
        renderer.shadowCastingMode = shadowCastingMode;
        renderer.receiveShadows = receiveShadows;
        if (subMeshCount <= 1)
        {
            renderer.sharedMaterial = material;
            return;
        }

        Il2CppReferenceArray<Material> materials = new Il2CppReferenceArray<Material>(subMeshCount);
        for (int i = 0; i < subMeshCount; i++)
            materials[i] = material;
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

    public static Vector2List Slots(int vertexCount)
    {
        Vector2List slots = new Vector2List(vertexCount);
        Vector2 slot = new Vector2(GhostBodySlot, 0f);
        for (int i = 0; i < vertexCount; i++)
            slots.Add(slot);
        return slots;
    }

    public void Destroy()
    {
        foreach (Mesh stream in slotStreams.Values)
            Object.Destroy(stream);
    }
}
