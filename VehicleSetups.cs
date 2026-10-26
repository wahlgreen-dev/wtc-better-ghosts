using System;
using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public class VehicleSetup
{
    public readonly string VehicleName;
    public readonly string[] PartPaths;
    public readonly Animator LegAnimator;

    public VehicleSetup(string vehicleName, params string[] partPaths)
    {
        VehicleName = vehicleName;
        PartPaths = partPaths;
    }

    public VehicleSetup(string vehicleName, Animator legAnimator)
    {
        VehicleName = vehicleName;
        PartPaths = Array.Empty<string>();
        LegAnimator = legAnimator;
    }
}

public static class VehicleSetups
{
    private static readonly Dictionary<string, VehicleSetup> byVehicleName = new();

    static VehicleSetups()
    {
        Add(new("Car with giraffe wheels Variant", "Rigidbody/Visuals/Components/Lamppost wheel", "Rigidbody/Visuals/Components/Lamppost wheel (2)"));
    }

    public static VehicleSetup Find(Transform vehicle)
    {
        string name = vehicle.name.Replace("(Clone)", "").Trim();
        if (byVehicleName.TryGetValue(name, out VehicleSetup setup))
            return setup;

        CarWithLegs legs = vehicle.GetComponentInChildren<CarWithLegs>(true);
        if (legs == null)
            return null;

        return new VehicleSetup(name, legs.animator);
    }

    private static void Add(VehicleSetup setup)
    {
        byVehicleName[setup.VehicleName] = setup;
    }
}
