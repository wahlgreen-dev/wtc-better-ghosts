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
    public readonly Transform PhysicsRig;
    public readonly bool SpinWheels;
    public readonly string RiderPath;

    private VehicleSetup(string vehicleName, string riderPath)
    {
        VehicleName = vehicleName;
        PartPaths = Array.Empty<string>();
        RiderPath = riderPath;
    }

    public static VehicleSetup WithRider(string vehicleName, string riderPath)
    {
        return new VehicleSetup(vehicleName, riderPath);
    }

    public VehicleSetup(string vehicleName, bool spinWheels)
    {
        VehicleName = vehicleName;
        PartPaths = Array.Empty<string>();
        SpinWheels = spinWheels;
    }

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

    public VehicleSetup(string vehicleName, Transform physicsRig)
    {
        VehicleName = vehicleName;
        PartPaths = Array.Empty<string>();
        PhysicsRig = physicsRig;
    }

    public Transform RigRoot => LegAnimator != null ? LegAnimator.transform : PhysicsRig;
}

public static class VehicleSetups
{
    private const string RiderName = "Car visuals moved by code";

    private static readonly Dictionary<string, VehicleSetup> byVehicleName = new();

    static VehicleSetups()
    {
        Add(new("Car with giraffe wheels Variant", "Rigidbody/Visuals/Components/Lamppost wheel", "Rigidbody/Visuals/Components/Lamppost wheel (2)"));
        Add(new("Car with keyboards Variant", spinWheels: true));
        Add(new("Car with keyboards ASMR Variant", spinWheels: true));
    }

    public static VehicleSetup Find(Transform vehicle)
    {
        string name = vehicle.name.Replace("(Clone)", "").Trim();
        if (byVehicleName.TryGetValue(MatchKey(name), out VehicleSetup setup))
            return setup;

        CarWithLegs legs = vehicle.GetComponentInChildren<CarWithLegs>(true);
        if (legs != null)
            return new VehicleSetup(name, legs.animator);

        Transform physicsRig = FindPhysicsRig(vehicle);
        if (physicsRig != null)
            return new VehicleSetup(name, physicsRig);

        if (TransformPaths.Find(vehicle, RiderName) != null)
            return VehicleSetup.WithRider(name, RiderName);

        return new VehicleSetup(name);
    }

    private static Transform FindPhysicsRig(Transform vehicle)
    {
        FrogCar frog = vehicle.GetComponentInChildren<FrogCar>(true);
        if (frog == null || frog.jumpVisualRigidBodies == null || frog.jumpVisualRigidBodies.Count == 0)
            return null;

        Animator rigAnimator = frog.jumpVisualRigidBodies[0].GetComponentInParent<Animator>(true);
        if (rigAnimator == null)
            return null;

        return rigAnimator.transform;
    }

    private static void Add(VehicleSetup setup)
    {
        byVehicleName[MatchKey(setup.VehicleName)] = setup;
    }

    private static string MatchKey(string vehicleName)
    {
        const string variantSuffix = " Variant";
        if (!vehicleName.EndsWith(variantSuffix))
            return vehicleName;

        return vehicleName.Substring(0, vehicleName.Length - variantSuffix.Length).TrimEnd();
    }
}
