using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;
using ParticleSystemList = Il2CppSystem.Collections.Generic.List<UnityEngine.ParticleSystem>;

namespace BetterGhosts;

public class PowerEffects
{
    private static readonly HashSet<string> effectComponents = new() { "Transform", "MeshFilter", "MeshRenderer", "Fire", "Billboard", "ParticleSystem", "ParticleSystemRenderer" };

    private class DrivenFire
    {
        public Fire Fire;
        public Vector3 ContainerScale;
    }

    private readonly List<DrivenFire> fires = new();
    private readonly List<ParticleSystem> smoke = new();
    private readonly List<ParticleSystem> bursts = new();
    private readonly List<Transform> keptRoots = new();
    private bool active;

    public bool Keeps(Transform transform)
    {
        foreach (Transform keptRoot in keptRoots)
        {
            if (transform.IsChildOf(keptRoot))
                return true;
        }
        return false;
    }

    public void AddFromCopy(GameObject copy, GameObject source)
    {
        var copyFires = copy.GetComponentsInChildren<Fire>(true);
        var sourceFires = source.GetComponentsInChildren<Fire>(true);
        for (int i = 0; i < copyFires.Length; i++)
        {
            keptRoots.Add(copyFires[i].transform);
            if (!sourceFires[i].startOn && sourceFires[i].gameObject.activeInHierarchy)
                fires.Add(new DrivenFire { Fire = copyFires[i], ContainerScale = sourceFires[i].containerScale });
        }

        var blocks = copy.GetComponentsInChildren<JetpackBuildingBlock>(true);
        var sourceBlocks = source.GetComponentsInChildren<JetpackBuildingBlock>(true);
        for (int i = 0; i < blocks.Length; i++)
        {
            if (!sourceBlocks[i].gameObject.activeInHierarchy)
                continue;

            AddAll(blocks[i].smoke, smoke);
            AddAll(blocks[i].burst, bursts);
            AddAll(blocks[i].burstRing, bursts);
        }

        foreach (ParticleSystem system in smoke)
            keptRoots.Add(system.transform);
        foreach (ParticleSystem system in bursts)
            keptRoots.Add(system.transform);
    }

    public void AddDetachedFire(Fire source, Transform anchor, Transform parent)
    {
        GameObject copy = CopyDetached(source.gameObject, anchor, parent);
        if (!source.startOn)
            fires.Add(new DrivenFire { Fire = copy.GetComponent<Fire>(), ContainerScale = source.containerScale });
    }

    public void AddDetachedBlock(JetpackBuildingBlock block, Transform anchor, Transform parent)
    {
        AddDetachedParticles(block.smoke, smoke, anchor, parent);
        AddDetachedParticles(block.burst, bursts, anchor, parent);
        AddDetachedParticles(block.burstRing, bursts, anchor, parent);
    }

    private static void AddDetachedParticles(ParticleSystemList sources, List<ParticleSystem> target, Transform anchor, Transform parent)
    {
        List<ParticleSystem> found = new();
        AddAll(sources, found);
        foreach (ParticleSystem source in found)
        {
            if (source.GetComponentInParent<Fire>(true) != null)
                continue;

            target.Add(CopyDetached(source.gameObject, anchor, parent).GetComponent<ParticleSystem>());
        }
    }

    private static GameObject CopyDetached(GameObject source, Transform anchor, Transform parent)
    {
        GameObject holder = new GameObject("BetterGhostEffectHolder");
        holder.SetActive(false);
        GameObject copy = Object.Instantiate(source, holder.transform);
        copy.name = source.name;

        ComponentStripper.Strip(copy, effectComponents, transform => false);
        ComponentStripper.SetLayer(copy.transform, parent.gameObject.layer);
        copy.transform.SetParent(parent, false);
        RelativePose.Copy(copy.transform, source.transform, anchor);
        Object.Destroy(holder);
        return copy;
    }

    public void SetActive(bool active)
    {
        if (active == this.active)
            return;

        this.active = active;
        if (active)
            StartBoosting();
        else
            StopBoosting();
    }

    private void StartBoosting()
    {
        foreach (DrivenFire driven in fires)
        {
            driven.Fire.containerScale = driven.ContainerScale;
            driven.Fire.TurnOn();
        }
        foreach (ParticleSystem system in smoke)
            system.Play();
        foreach (ParticleSystem system in bursts)
        {
            system.gameObject.SetActive(true);
            system.Play();
        }
    }

    private void StopBoosting()
    {
        foreach (DrivenFire driven in fires)
            driven.Fire.TurnOff();
        foreach (ParticleSystem system in smoke)
            system.Stop();
    }

    private static void AddAll(ParticleSystemList source, List<ParticleSystem> target)
    {
        if (source == null)
            return;

        for (int i = 0; i < source.Count; i++)
        {
            if (source[i] != null)
                target.Add(source[i]);
        }
    }
}
