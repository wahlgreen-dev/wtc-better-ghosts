using System.Collections.Generic;
using Il2Cpp;
using UnityEngine;

namespace BetterGhosts;

public static class VehicleSetups
{
    private static readonly Dictionary<string, VehicleSetup> byVehicleName = new();

    static VehicleSetups()
    {
        Add(VehicleSetup.WithRecordedParts("Car with giraffe wheels Variant", "Rigidbody/Visuals/Components/Lamppost wheel", "Rigidbody/Visuals/Components/Lamppost wheel (2)"));
    }

    public static VehicleSetup Find(Transform vehicle)
    {
        string name = vehicle.name.Replace("(Clone)", "").Trim();
        if (byVehicleName.TryGetValue(MatchKey(name), out VehicleSetup setup))
            return setup;

        CarWithLegs legs = vehicle.GetComponentInChildren<CarWithLegs>(true);
        if (legs != null && legs.animator != null)
            return VehicleSetup.WithRig(name, legs.animator.transform);

        CarWithLegsSkater skater = vehicle.GetComponentInChildren<CarWithLegsSkater>(true);
        if (skater != null && skater.animator != null)
            return VehicleSetup.WithRig(name, skater.animator.transform);

        DoubleSwingers swingers = vehicle.GetComponentInChildren<DoubleSwingers>(true);
        if (swingers != null && swingers.childContainer != null)
            return VehicleSetup.WithRig(name, swingers.childContainer);

        StiltsController stilts = vehicle.GetComponentInChildren<StiltsController>(true);
        if (stilts != null)
            return VehicleSetup.WithRecordedFrame(name, TransformPaths.RelativePath(stilts.root.transform, vehicle), StiltPaths(vehicle, stilts));

        Transform physicsRig = FindPhysicsRig(vehicle);
        if (physicsRig != null)
            return VehicleSetup.WithPhysicsRig(name, physicsRig);

        Transform rider = TransformPaths.Find(vehicle, RiderPose.RiderName);
        if (rider != null)
            return VehicleSetup.WithRig(name, rider);

        CarpController carp = vehicle.GetComponentInChildren<CarpController>(true);
        if (carp != null && carp.finAnim != null)
            return VehicleSetup.WithRig(name, carp.finAnim.transform);

        TransferVelocityToAnimationProperty velocityAnimation = vehicle.GetComponentInChildren<TransferVelocityToAnimationProperty>(true);
        if (velocityAnimation != null && velocityAnimation.animator != null)
            return VehicleSetup.WithRig(name, velocityAnimation.animator.transform);

        return VehicleSetup.ShapeOnly(name);
    }

    private static string[] StiltPaths(Transform vehicle, StiltsController controller)
    {
        List<string> paths = new();
        for (int i = 0; i < controller.stilts.Count; i++)
            paths.Add(TransformPaths.RelativePath(controller.stilts[i].Rigid.transform, vehicle));
        return paths.ToArray();
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
