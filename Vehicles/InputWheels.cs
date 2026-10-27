using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public static class InputWheels
{
    public static Transform[] Find(Transform vehicle, Transform rigRoot)
    {
        var inputWheels = vehicle.GetComponentsInChildren<InputWheel>(false);
        List<Transform> found = new();
        for (int i = 0; i < inputWheels.Length; i++)
        {
            Transform wheel = inputWheels[i].transform;
            if (wheel.GetComponent<HingeJoint>() == null || (rigRoot != null && wheel.IsChildOf(rigRoot)))
                continue;
            found.Add(wheel);
        }
        return found.ToArray();
    }
}
