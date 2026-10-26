using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;
using UnityEngine.Rendering;
using Vector2List = Il2CppSystem.Collections.Generic.List<UnityEngine.Vector2>;

namespace BetterGhosts;

public class CopiedRig
{
    private static readonly HashSet<string> visualComponents = new() { "Transform", "Animator", "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer" };
    private static readonly HashSet<string> physicsComponents = new() { "Transform", "Animator", "MeshFilter", "MeshRenderer", "SkinnedMeshRenderer", "Rigidbody", "ConfigurableJoint", "HingeJoint", "CharacterJoint", "FixedJoint", "SpringJoint" };

    private class BakedSkin
    {
        public SkinnedMeshRenderer Skin;
        public Mesh Mesh;
        public Vector2List PaletteSlots;
    }

    private readonly List<BakedSkin> skins = new();
    private readonly PowerEffects power;

    public Animator Animator { get; }

    public Transform Root { get; }

    public CopiedRig(Transform source, Transform body, Transform parent, GhostPalette palette, PowerEffects power, Transform physicsContainer)
    {
        this.power = power;
        bool keepPhysics = physicsContainer != null;
        GameObject holder = new GameObject("BetterGhostRigHolder");
        holder.SetActive(false);
        GameObject copy = Object.Instantiate(source.gameObject, holder.transform);
        copy.name = source.gameObject.name;
        Root = copy.transform;

        power.AddFromCopy(copy, source.gameObject);
        RemoveBillboards(copy.transform);
        ComponentStripper.Strip(copy, keepPhysics ? physicsComponents : visualComponents, power.Keeps);
        ComponentStripper.SetLayer(copy.transform, parent.gameObject.layer);
        if (keepPhysics)
            PreparePhysics(copy, source.gameObject, parent.gameObject);

        copy.transform.SetParent(parent, false);
        RelativePose.Copy(copy.transform, source.transform, body);
        Object.Destroy(holder);

        PaintMeshRenderers(copy, palette);
        PrepareSkins(copy, palette);
        if (keepPhysics)
            DetachDynamicBodies(copy, physicsContainer);

        Animator = copy.GetComponent<Animator>();
        if (Animator == null)
            return;

        Animator.applyRootMotion = false;
        Animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
        Animator.enabled = !keepPhysics;
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

    private static void PreparePhysics(GameObject copy, GameObject source, GameObject anchorObject)
    {
        var copyBodies = copy.GetComponentsInChildren<Rigidbody>(true);
        var sourceBodies = source.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < copyBodies.Length; i++)
            CopyMassProperties(sourceBodies[i], copyBodies[i]);

        var hinges = copy.GetComponentsInChildren<HingeJoint>(true);
        for (int i = 0; i < hinges.Length; i++)
            FreezeHinged(hinges[i]);

        Rigidbody anchor = anchorObject.AddComponent<Rigidbody>();
        anchor.isKinematic = true;
        var joints = copy.GetComponentsInChildren<Joint>(true);
        for (int i = 0; i < joints.Length; i++)
        {
            Rigidbody connected = joints[i].connectedBody;
            if (connected != null && connected.transform.IsChildOf(copy.transform))
                continue;

            joints[i].connectedBody = anchor;
        }
    }

    private static void FreezeHinged(HingeJoint hinge)
    {
        Rigidbody body = hinge.GetComponent<Rigidbody>();
        Object.DestroyImmediate(hinge);
        Object.DestroyImmediate(body);
    }

    private static void DetachDynamicBodies(GameObject copy, Transform physicsContainer)
    {
        List<Transform> detached = new();
        var bodies = copy.GetComponentsInChildren<Rigidbody>(true);
        for (int i = 0; i < bodies.Length; i++)
        {
            if (!bodies[i].isKinematic && !HasDynamicAncestor(bodies[i].transform, copy.transform))
                detached.Add(bodies[i].transform);
        }

        foreach (Transform transform in detached)
            transform.SetParent(physicsContainer, true);
    }

    private static bool HasDynamicAncestor(Transform transform, Transform root)
    {
        for (Transform t = transform.parent; t != null && t != root.parent; t = t.parent)
        {
            Rigidbody body = t.GetComponent<Rigidbody>();
            if (body != null && !body.isKinematic)
                return true;
        }
        return false;
    }

    private static void CopyMassProperties(Rigidbody source, Rigidbody copy)
    {
        copy.mass = source.mass;
        copy.linearDamping = source.linearDamping;
        copy.angularDamping = source.angularDamping;
        copy.useGravity = source.useGravity;
        copy.isKinematic = source.isKinematic;
        copy.interpolation = source.interpolation;
        copy.maxAngularVelocity = source.maxAngularVelocity;
        copy.centerOfMass = source.centerOfMass;
        copy.inertiaTensor = source.inertiaTensor;
        copy.inertiaTensorRotation = source.inertiaTensorRotation;
    }

    private void RemoveBillboards(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            Transform transform = renderers[i].transform;
            if (!power.Keeps(transform) && transform.GetComponent<Billboard>() != null)
                Object.DestroyImmediate(renderers[i]);
        }
    }

    private void PaintMeshRenderers(GameObject root, GhostPalette palette)
    {
        var renderers = root.GetComponentsInChildren<MeshRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            MeshRenderer renderer = renderers[i];
            if (power.Keeps(renderer.transform))
                continue;

            MeshFilter filter = renderer.GetComponent<MeshFilter>();
            if (filter == null || filter.sharedMesh == null || renderer.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
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
        var skinnedRenderers = root.GetComponentsInChildren<SkinnedMeshRenderer>(true);
        for (int i = 0; i < skinnedRenderers.Length; i++)
        {
            SkinnedMeshRenderer skin = skinnedRenderers[i];
            bool wasEnabled = skin.enabled;
            skin.enabled = false;
            if (!wasEnabled || skin.shadowCastingMode == ShadowCastingMode.ShadowsOnly)
                continue;

            if (skin.bones.Length == 0)
            {
                palette.PaintWithoutSlots(skin, skin.sharedMesh.subMeshCount);
                skin.updateWhenOffscreen = true;
                skin.enabled = true;
                continue;
            }

            GameObject bakedObject = new GameObject(skin.gameObject.name + " (baked)");
            bakedObject.layer = skin.gameObject.layer;
            bakedObject.transform.SetParent(skin.transform, false);

            Mesh mesh = new Mesh();
            mesh.MarkDynamic();
            bakedObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            palette.Paint(bakedObject.AddComponent<MeshRenderer>(), skin.sharedMesh.subMeshCount);

            skins.Add(new BakedSkin { Skin = skin, Mesh = mesh, PaletteSlots = palette.Slots(skin.sharedMesh.vertexCount) });
        }
        BakeSkins();
    }
}
