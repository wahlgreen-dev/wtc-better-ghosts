using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class HotWheels
{
    private readonly List<(Transform pivot, float radius)> wheels = new();
    private readonly float actionSpinMultiplier;

    public HotWheels(HotWheelsController controller)
    {
        actionSpinMultiplier = controller.actionButtonSpinMultiplier;
    }

    public void Add(Transform pivot, float radius)
    {
        if (radius > 0f)
            wheels.Add((pivot, radius));
    }

    public void Update(Vector3 velocity, Vector3 forward, bool actionPressed, float deltaTime)
    {
        float speed = Vector3.Dot(velocity, forward) * (actionPressed ? actionSpinMultiplier : 1f);
        foreach ((Transform pivot, float radius) in wheels)
            pivot.Rotate(Vector3.right, speed / radius * Mathf.Rad2Deg * deltaTime, Space.Self);
    }
}
