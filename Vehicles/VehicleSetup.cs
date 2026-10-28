using System;
using UnityEngine;

namespace BetterGhosts;

public class VehicleSetup
{
    public readonly string VehicleName;
    public readonly string[] RecordedPartPaths;
    public readonly bool RecordsRotationOnly;
    public readonly string FramePath;
    public readonly Transform RigRoot;
    public readonly bool RigKeepsPhysics;

    private VehicleSetup(string vehicleName, string[] recordedPartPaths, bool recordsRotationOnly, string framePath, Transform rigRoot, bool rigKeepsPhysics)
    {
        VehicleName = vehicleName;
        RecordedPartPaths = recordedPartPaths;
        RecordsRotationOnly = recordsRotationOnly;
        FramePath = framePath;
        RigRoot = rigRoot;
        RigKeepsPhysics = rigKeepsPhysics;
    }

    public bool HasFrame => FramePath.Length > 0;

    public static VehicleSetup ShapeOnly(string vehicleName)
    {
        return new VehicleSetup(vehicleName, Array.Empty<string>(), false, "", null, false);
    }

    public static VehicleSetup WithRig(string vehicleName, Transform rigRoot)
    {
        return new VehicleSetup(vehicleName, Array.Empty<string>(), false, "", rigRoot, false);
    }

    public static VehicleSetup WithPhysicsRig(string vehicleName, Transform rigRoot)
    {
        return new VehicleSetup(vehicleName, Array.Empty<string>(), false, "", rigRoot, true);
    }

    public static VehicleSetup WithRecordedParts(string vehicleName, params string[] partPaths)
    {
        return new VehicleSetup(vehicleName, partPaths, false, "", null, false);
    }

    public static VehicleSetup WithRecordedFrame(string vehicleName, string framePath, string[] rotatingPartPaths)
    {
        return new VehicleSetup(vehicleName, rotatingPartPaths, true, framePath, null, false);
    }
}
