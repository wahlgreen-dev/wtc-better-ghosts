using System;
using System.Collections.Generic;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterGhosts;

public static class ComponentStripper
{
    private const int RemovalStages = 4;

    public static void Strip(GameObject root, HashSet<string> keptTypes, Func<Transform, bool> keepsTransform)
    {
        List<Component>[] stages = new List<Component>[RemovalStages];
        for (int stage = 0; stage < RemovalStages; stage++)
            stages[stage] = new List<Component>();

        var components = root.GetComponentsInChildren<Component>(true);
        for (int i = 0; i < components.Length; i++)
        {
            Component component = components[i];
            if (component == null)
                continue;

            string typeName = component.GetIl2CppType().Name;
            if (keptTypes.Contains(typeName) || keepsTransform(component.transform))
                continue;

            stages[RemovalStage(component, typeName)].Add(component);
        }

        List<Component> refused = new();
        foreach (List<Component> stage in stages)
        {
            foreach (Component component in stage)
            {
                Object.DestroyImmediate(component);
                if (component != null)
                    refused.Add(component);
            }
        }

        foreach (Component component in refused)
        {
            if (component != null)
                Object.DestroyImmediate(component);
        }
    }

    public static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayer(root.GetChild(i), layer);
    }

    private static int RemovalStage(Component component, string typeName)
    {
        if (component.TryCast<MonoBehaviour>() != null || component.TryCast<Joint>() != null)
            return 0;
        if (typeName == "ParticleSystem")
            return 2;
        if (typeName == "Rigidbody")
            return 3;
        return 1;
    }
}
