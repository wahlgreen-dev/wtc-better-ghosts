using UnityEngine;

namespace BetterGhosts;

public static class TransformPaths
{
    public static Transform Find(Transform root, string path)
    {
        Transform current = root;
        foreach (string name in path.Split('/'))
        {
            current = FindChild(current, name);
            if (current == null)
                return null;
        }
        return current;
    }

    public static Transform FindDescendant(Transform root, string name)
    {
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == name)
                return child;

            Transform found = FindDescendant(child, name);
            if (found != null)
                return found;
        }
        return null;
    }

    public static bool HasComponentNamed(Transform transform, string typeName)
    {
        var components = transform.GetComponents<Component>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] != null && components[i].GetIl2CppType().Name == typeName)
                return true;
        }
        return false;
    }

    private static Transform FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }
        return null;
    }
}
