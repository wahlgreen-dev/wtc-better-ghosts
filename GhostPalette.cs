using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Rendering;

namespace BetterGhosts;

public class GhostPalette
{
    public const int SlotChannel = 3;
    const float GhostBodySlot = 34.09375f;

    readonly Material material;
    readonly ShadowCastingMode shadowCastingMode;
    readonly bool receiveShadows;
    readonly Dictionary<int, Mesh> slotStreams = new();

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

        var materials = new Il2CppReferenceArray<Material>(subMeshCount);
        for (int i = 0; i < subMeshCount; i++)
            materials[i] = material;
        renderer.sharedMaterials = materials;
    }

    public Mesh SlotStream(int vertexCount)
    {
        if (slotStreams.TryGetValue(vertexCount, out var stream))
            return stream;

        stream = new Mesh();
        var layout = new Il2CppStructArray<VertexAttributeDescriptor>(1);
        layout[0] = new VertexAttributeDescriptor(VertexAttribute.TexCoord3, VertexAttributeFormat.Float32, 2, 0);
        stream.SetVertexBufferParams(vertexCount, layout);
        stream.SetUVs(SlotChannel, Slots(vertexCount));
        slotStreams[vertexCount] = stream;
        return stream;
    }

    public static Il2CppSystem.Collections.Generic.List<Vector2> Slots(int vertexCount)
    {
        var slots = new Il2CppSystem.Collections.Generic.List<Vector2>(vertexCount);
        var slot = new Vector2(GhostBodySlot, 0f);
        for (int i = 0; i < vertexCount; i++)
            slots.Add(slot);
        return slots;
    }

    public void Destroy()
    {
        foreach (var stream in slotStreams.Values)
        {
            if (stream != null)
                Object.Destroy(stream);
        }
    }
}
