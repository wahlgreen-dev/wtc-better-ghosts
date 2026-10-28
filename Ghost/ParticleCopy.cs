using System.Collections.Generic;
using UnityEngine;

namespace BetterGhosts;

public static class ParticleCopy
{
    private static readonly HashSet<string> keptTypes = new() { "Transform", "ParticleSystem", "ParticleSystemRenderer" };

    public static ParticleSystem Create(ParticleSystem source, int layer)
    {
        GameObject holder = new GameObject("ParticleHolder");
        holder.SetActive(false);
        GameObject copy = Object.Instantiate(source.gameObject, holder.transform);
        copy.name = source.gameObject.name;
        ComponentStripper.Strip(copy, keptTypes, _ => false);
        ComponentStripper.SetLayer(copy.transform, layer);
        copy.transform.SetParent(null, false);
        Object.Destroy(holder);

        ParticleSystem particles = copy.GetComponent<ParticleSystem>();
        particles.Clear(true);
        return particles;
    }
}
