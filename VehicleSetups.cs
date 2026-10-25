using System.Collections.Generic;

namespace BetterGhosts;

public class VehicleSetup
{
    public readonly string VehicleName;
    public readonly string[] PartPaths;

    public VehicleSetup(string vehicleName, params string[] partPaths)
    {
        VehicleName = vehicleName;
        PartPaths = partPaths;
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

    public static VehicleSetup Find(string vehicleObjectName)
    {
        return byVehicleName.TryGetValue(NormalizeName(vehicleObjectName), out var setup) ? setup : null;
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
