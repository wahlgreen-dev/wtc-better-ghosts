using System.Collections.Generic;
using Il2CppSpeed.Level;
using UnityEngine;

namespace BetterGhosts;

public static class GoalObjects
{
    public static Transform[] Find(Transform vehicle)
    {
        List<Transform> found = new();

        var triggerConditions = Object.FindObjectsOfType<GetObjectsToTriggerWinCondition>();
        for (int i = 0; i < triggerConditions.Length; i++)
        {
            var goalObjects = triggerConditions[i].goalObjects;
            if (goalObjects == null)
                continue;

            for (int j = 0; j < goalObjects.Count; j++)
            {
                if (goalObjects[j] != null)
                    Add(found, goalObjects[j].transform, vehicle);
            }
        }

        var positionConditions = Object.FindObjectsOfType<GetObjectsToPositionWinCondition>();
        for (int i = 0; i < positionConditions.Length; i++)
        {
            var goalObjects = positionConditions[i].goalObjects;
            if (goalObjects == null)
                continue;

            for (int j = 0; j < goalObjects.Count; j++)
            {
                if (goalObjects[j] != null)
                    Add(found, goalObjects[j].go, vehicle);
            }
        }

        return found.ToArray();
    }

    public static string[] Names(Transform[] goalObjects)
    {
        string[] names = new string[goalObjects.Length];
        for (int i = 0; i < goalObjects.Length; i++)
            names[i] = goalObjects[i].name;
        return names;
    }

    private static void Add(List<Transform> found, Transform goalObject, Transform vehicle)
    {
        if (goalObject == null || goalObject.IsChildOf(vehicle) || found.Contains(goalObject))
            return;

        found.Add(goalObject);
    }
}
