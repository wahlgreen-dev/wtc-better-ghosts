using Il2CppSpeed.GhostCar;
using UnityEngine;

namespace BetterGhosts;

public class GhostRecorder
{
    private readonly GhostCarManager manager;
    private readonly Transform body;
    private readonly Transform[] parts;
    private readonly Transform[] wheels;
    private readonly float[] wheelAngles;
    private readonly Quaternion[] lastWheelRotations;
    private int gameSamplesSeen;

    public PartRecording Recording { get; }

    public GhostRecorder(GhostCarManager manager, Transform vehicle, Transform body, Transform[] parts, Transform[] wheels, VehicleSetup setup)
    {
        this.manager = manager;
        this.body = body;
        this.parts = parts;
        this.wheels = wheels;
        wheelAngles = new float[wheels.Length];
        lastWheelRotations = new Quaternion[wheels.Length];

        WheelSpin[] spins = new WheelSpin[wheels.Length];
        for (int i = 0; i < wheels.Length; i++)
        {
            spins[i] = WheelSpin.Measure(wheels[i], wheels[i].GetComponent<HingeJoint>(), body, vehicle);
            lastWheelRotations[i] = spins[i].ReferenceRotation;
        }
        Recording = new PartRecording(setup.VehicleName, setup.PartPaths, spins);
    }

    public void Update()
    {
        TrackWheels();

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

    private void TrackWheels()
    {
        Quaternion bodyRotationInverse = Quaternion.Inverse(body.rotation);
        for (int i = 0; i < wheels.Length; i++)
        {
            Quaternion relative = bodyRotationInverse * wheels[i].rotation;
            (relative * Quaternion.Inverse(lastWheelRotations[i])).ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f)
                angle -= 360f;

            wheelAngles[i] += angle * Mathf.Sign(Vector3.Dot(axis, Recording.Wheels[i].Axis));
            lastWheelRotations[i] = relative;
        }
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
        Recording.Add(positions, rotations, (float[])wheelAngles.Clone());
    }
}
