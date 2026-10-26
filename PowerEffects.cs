using System.Collections.Generic;
using Il2Cpp;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;

namespace BetterGhosts;

public class PowerEffects
{
    private static readonly HashSet<string> fireComponents = new() { "Transform", "MeshFilter", "MeshRenderer", "Fire", "Billboard", "ParticleSystem", "ParticleSystemRenderer" };

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
        Il2CppArrayBase<Fire> copyFires = copy.GetComponentsInChildren<Fire>(true);
        Il2CppArrayBase<Fire> sourceFires = source.GetComponentsInChildren<Fire>(true);
        for (int i = 0; i < copyFires.Length; i++)
        {
            keptRoots.Add(copyFires[i].transform);
            if (!sourceFires[i].startOn)
                fires.Add(new DrivenFire { Fire = copyFires[i], ContainerScale = sourceFires[i].containerScale });
        }

        Il2CppArrayBase<JetpackBuildingBlock> blocks = copy.GetComponentsInChildren<JetpackBuildingBlock>(true);
        for (int i = 0; i < blocks.Length; i++)
        {
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
        GameObject holder = new GameObject("BetterGhostFireHolder");
        holder.SetActive(false);
        GameObject copy = Object.Instantiate(source.gameObject, holder.transform);
        copy.name = source.gameObject.name;

        ComponentStripper.Strip(copy, fireComponents, transform => false);
        ComponentStripper.SetLayer(copy.transform, parent.gameObject.layer);
        copy.transform.SetParent(parent, false);
        RelativePose.Copy(copy.transform, source.transform, anchor);
        Object.Destroy(holder);

        if (!source.startOn)
            fires.Add(new DrivenFire { Fire = copy.GetComponent<Fire>(), ContainerScale = source.containerScale });
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

    private static void AddAll<T>(Il2CppSystem.Collections.Generic.List<T> source, List<T> target) where T : Il2CppObjectBase
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
