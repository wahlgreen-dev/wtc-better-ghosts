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
        PartPaths = new string[0];
        LegAnimator = legAnimator;
    }
}

public static class VehicleSetups
{
    static readonly Dictionary<string, VehicleSetup> byVehicleName = new();

    static VehicleSetups()
    {
        Add(new VehicleSetup("Car with giraffe wheels Variant",
            "Rigidbody/Visuals/Components/Lamppost wheel",
            "Rigidbody/Visuals/Components/Lamppost wheel (2)"));
    }

    public static VehicleSetup Find(Transform vehicle)
    {
        var name = NormalizeName(vehicle.name);
        if (byVehicleName.TryGetValue(name, out var setup))
            return setup;

        var legs = vehicle.GetComponentInChildren<CarWithLegs>(true);
        if (legs != null && legs.animator != null)
            return new VehicleSetup(name, legs.animator);

        return null;
    }

    public static string NormalizeName(string objectName)
    {
        return objectName.Replace("(Clone)", "").Trim();
    }

    static void Add(VehicleSetup setup)
    {
        byVehicleName[setup.VehicleName] = setup;
    }
}
