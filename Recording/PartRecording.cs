using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BetterGhosts;

public class PartRecording
{
    public readonly string VehicleName;
    public readonly string FramePath;
    public readonly bool PartsRotateOnly;
    public readonly string[] PartPaths;
    public readonly string[] GoalObjectNames;
    public readonly List<Vector3> FramePositions = new();
    public readonly List<Quaternion> FrameRotations = new();
    public readonly List<Vector3[]> Positions = new();
    public readonly List<Quaternion[]> Rotations = new();
    public readonly List<Vector3[]> GoalPositions = new();

    public int SampleCount => Positions.Count;

    public bool HasFrame => FramePath.Length > 0;

    public PartRecording(string vehicleName, string framePath, bool partsRotateOnly, string[] partPaths, string[] goalObjectNames)
    {
        VehicleName = vehicleName;
        FramePath = framePath;
        PartsRotateOnly = partsRotateOnly;
        PartPaths = partPaths;
        GoalObjectNames = goalObjectNames;
    }

    public PartRecording(VehicleSetup setup, string[] goalObjectNames) : this(setup.VehicleName, setup.FramePath, setup.RecordsRotationOnly, setup.RecordedPartPaths, goalObjectNames)
    {
    }

    public bool Matches(VehicleSetup setup)
    {
        return VehicleName == setup.VehicleName && FramePath == setup.FramePath && PartsRotateOnly == setup.RecordsRotationOnly && PartPaths.SequenceEqual(setup.RecordedPartPaths);
    }

    public void Add(Vector3 framePosition, Quaternion frameRotation, Vector3[] positions, Quaternion[] rotations, Vector3[] goalPositions)
    {
        FramePositions.Add(framePosition);
        FrameRotations.Add(frameRotation);
        Positions.Add(positions);
        Rotations.Add(rotations);
        GoalPositions.Add(goalPositions);
    }

    public void Clear()
    {
        FramePositions.Clear();
        FrameRotations.Clear();
        Positions.Clear();
        Rotations.Clear();
        GoalPositions.Clear();
    }
}
