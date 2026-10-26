using Il2CppSpeed.GhostCar;
using UnityEngine;

namespace BetterGhosts;

public class GhostRecorder
{
    private readonly GhostCarManager manager;
    private readonly Transform body;
    private readonly Transform[] parts;
    private int gameSamplesSeen;

    public PartRecording Recording { get; }

    public GhostRecorder(GhostCarManager manager, Transform body, Transform[] parts, VehicleSetup setup)
    {
        this.manager = manager;
        this.body = body;
        this.parts = parts;
        Recording = new PartRecording(setup.VehicleName, setup.PartPaths);
    }

    public void Update()
    {
        var gameSamples = manager.ghostDataTempRecorded;
        if (gameSamples == null)
            return;

        int count = gameSamples.Count;
        if (count < gameSamplesSeen)
        {
            Recording.Clear();
            gameSamplesSeen = 0;
        }

        for (int i = gameSamplesSeen; i < count; i++)
            Capture();
        gameSamplesSeen = count;
    }

    private void Capture()
    {
        Quaternion bodyRotationInverse = Quaternion.Inverse(body.rotation);
        Vector3[] positions = new Vector3[parts.Length];
        Quaternion[] rotations = new Quaternion[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            positions[i] = body.InverseTransformPoint(parts[i].position);
            rotations[i] = bodyRotationInverse * parts[i].rotation;
        }
        Recording.Add(positions, rotations);
    }
}
