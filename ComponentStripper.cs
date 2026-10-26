using System;
using System.Collections.Generic;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using Object = UnityEngine.Object;

namespace BetterGhosts;

public static class ComponentStripper
{
    private const int MaxPasses = 5;

    public static void Strip(GameObject root, HashSet<string> keptTypes, Func<Transform, bool> keepsTransform)
    {
        for (int pass = 0; pass < MaxPasses; pass++)
        {
            bool removed = false;
            Il2CppArrayBase<Component> components = root.GetComponentsInChildren<Component>(true);
            for (int i = 0; i < components.Length; i++)
            {
                Component component = components[i];
                if (component == null || keptTypes.Contains(component.GetIl2CppType().Name) || keepsTransform(component.transform))
                    continue;
                if (!CanRemoveInPass(component, pass))
                    continue;

                Object.DestroyImmediate(component);
                removed = true;
            }

            if (!removed && pass > 2)
                return;
        }
    }

    public static void SetLayer(Transform root, int layer)
    {
        root.gameObject.layer = layer;
        for (int i = 0; i < root.childCount; i++)
            SetLayer(root.GetChild(i), layer);
    }

    private static bool CanRemoveInPass(Component component, int pass)
    {
        if (component.TryCast<MonoBehaviour>() != null || component.TryCast<Joint>() != null)
            return true;
        if (component.TryCast<Rigidbody>() != null)
            return pass >= 2;
        return pass >= 1;
    }
}
