using UnityEngine;

namespace BetterGhosts;

public static class TransformPaths
{
    public static Transform Find(Transform root, string path)
    {
        var current = root;
        foreach (var name in path.Split('/'))
        {
            current = FindChild(current, name);
            if (current == null)
                return null;
        }
        return current;
    }

    static Transform FindChild(Transform parent, string name)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);
            if (child.name == name)
                return child;
        }
        return null;
    }
}
