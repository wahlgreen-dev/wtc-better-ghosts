using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BetterGhosts;

public class PartRecording
{
    private const string Magic = "BGH";
    private const byte FormatVersion = 3;
    private const byte FormatVersionWithoutWheels = 2;
    private const int BitsPerValue = 10;
    private const ulong ValueMask = (1UL << BitsPerValue) - 1;
    private const float ValueSteps = ValueMask;
    private const float SmallestThreeLimit = 0.70710678f;

    public readonly string VehicleName;
    public readonly string[] PartPaths;
    public readonly List<Vector3[]> Positions = new();
    public readonly List<Quaternion[]> Rotations = new();
    public readonly WheelSpin[] Wheels;
    public readonly List<float[]> WheelAngles = new();

    public int SampleCount => Positions.Count;

    public PartRecording(string vehicleName, string[] partPaths, WheelSpin[] wheels)
    {
        VehicleName = vehicleName;
        PartPaths = partPaths;
        Wheels = wheels;
    }

    public bool Matches(VehicleSetup setup)
    {
        return VehicleName == setup.VehicleName && PartPaths.SequenceEqual(setup.PartPaths);
    }

    public void Add(Vector3[] positions, Quaternion[] rotations, float[] wheelAngles)
    {
        Positions.Add(positions);
        Rotations.Add(rotations);
        WheelAngles.Add(wheelAngles);
    }

    public void Clear()
    {
        Positions.Clear();
        Rotations.Clear();
        WheelAngles.Clear();
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using FileStream file = File.Create(path);
        using GZipStream gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal);
        using BinaryWriter writer = new BinaryWriter(gzip, Encoding.UTF8);

        writer.Write(Encoding.ASCII.GetBytes(Magic));
        writer.Write(FormatVersion);
        writer.Write(VehicleName);
        writer.Write(PartPaths.Length);
        foreach (string partPath in PartPaths)
            writer.Write(partPath);
        writer.Write(Wheels.Length);
        foreach (WheelSpin wheel in Wheels)
            WriteWheel(writer, wheel);
        writer.Write(SampleCount);

        Vector3[] mins = new Vector3[PartPaths.Length];
        Vector3[] ranges = new Vector3[PartPaths.Length];
        for (int part = 0; part < PartPaths.Length; part++)
        {
            (mins[part], ranges[part]) = PositionBounds(part);
            WriteVector(writer, mins[part]);
            WriteVector(writer, ranges[part]);
        }

        for (int sample = 0; sample < SampleCount; sample++)
        {
            for (int part = 0; part < PartPaths.Length; part++)
                writer.Write(PackPosition(Positions[sample][part], mins[part], ranges[part]) | PackRotation(Rotations[sample][part]) << 30);
            foreach (float angle in WheelAngles[sample])
                writer.Write(angle);
        }
    }

    public static PartRecording Load(string path)
    {
        using FileStream file = File.OpenRead(path);
        using GZipStream gzip = new GZipStream(file, CompressionMode.Decompress);
        using BinaryReader reader = new BinaryReader(gzip, Encoding.UTF8);

        if (Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length)) != Magic)
            return null;
        byte version = reader.ReadByte();
        if (version != FormatVersion && version != FormatVersionWithoutWheels)
            return null;

        string vehicleName = reader.ReadString();
        string[] partPaths = new string[reader.ReadInt32()];
        for (int part = 0; part < partPaths.Length; part++)
            partPaths[part] = reader.ReadString();
        WheelSpin[] wheels = new WheelSpin[version == FormatVersion ? reader.ReadInt32() : 0];
        for (int wheel = 0; wheel < wheels.Length; wheel++)
            wheels[wheel] = ReadWheel(reader);
        int sampleCount = reader.ReadInt32();

        Vector3[] mins = new Vector3[partPaths.Length];
        Vector3[] ranges = new Vector3[partPaths.Length];
        for (int part = 0; part < partPaths.Length; part++)
        {
            mins[part] = ReadVector(reader);
            ranges[part] = ReadVector(reader);
        }

        PartRecording recording = new PartRecording(vehicleName, partPaths, wheels);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            Vector3[] positions = new Vector3[partPaths.Length];
            Quaternion[] rotations = new Quaternion[partPaths.Length];
            for (int part = 0; part < partPaths.Length; part++)
            {
                ulong packed = reader.ReadUInt64();
                positions[part] = UnpackPosition(packed, mins[part], ranges[part]);
                rotations[part] = UnpackRotation(packed >> 30);
            }

            float[] wheelAngles = new float[wheels.Length];
            for (int wheel = 0; wheel < wheels.Length; wheel++)
                wheelAngles[wheel] = reader.ReadSingle();
            recording.Add(positions, rotations, wheelAngles);
        }
        return recording;
    }

    private static void WriteWheel(BinaryWriter writer, WheelSpin wheel)
    {
        writer.Write(wheel.Path);
        WriteVector(writer, wheel.Axis);
        WriteVector(writer, wheel.Anchor);
        WriteVector(writer, wheel.ReferencePosition);
        Quaternion rotation = wheel.ReferenceRotation;
        writer.Write(rotation.x);
        writer.Write(rotation.y);
        writer.Write(rotation.z);
        writer.Write(rotation.w);
    }

    private static WheelSpin ReadWheel(BinaryReader reader)
    {
        string path = reader.ReadString();
        Vector3 axis = ReadVector(reader);
        Vector3 anchor = ReadVector(reader);
        Vector3 referencePosition = ReadVector(reader);
        Quaternion referenceRotation = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        return new WheelSpin(path, axis, anchor, referencePosition, referenceRotation);
    }

    private (Vector3 min, Vector3 range) PositionBounds(int part)
    {
        Vector3 min = Positions[0][part];
        Vector3 max = min;
        for (int sample = 1; sample < SampleCount; sample++)
        {
            min = Vector3.Min(min, Positions[sample][part]);
            max = Vector3.Max(max, Positions[sample][part]);
        }
        return (min, max - min);
    }

    private static ulong PackPosition(Vector3 position, Vector3 min, Vector3 range)
    {
        return Quantize(position.x, min.x, range.x) | Quantize(position.y, min.y, range.y) << BitsPerValue | Quantize(position.z, min.z, range.z) << (2 * BitsPerValue);
    }

    private static Vector3 UnpackPosition(ulong packed, Vector3 min, Vector3 range)
    {
        float x = Dequantize(packed & ValueMask, min.x, range.x);
        float y = Dequantize(packed >> BitsPerValue & ValueMask, min.y, range.y);
        float z = Dequantize(packed >> (2 * BitsPerValue) & ValueMask, min.z, range.z);
        return new Vector3(x, y, z);
    }

    private static ulong PackRotation(Quaternion rotation)
    {
        float[] components = { rotation.x, rotation.y, rotation.z, rotation.w };
        int largest = 0;
        for (int i = 1; i < 4; i++)
        {
            if (Mathf.Abs(components[i]) > Mathf.Abs(components[largest]))
                largest = i;
        }

        float sign = components[largest] < 0f ? -1f : 1f;
        ulong packed = (ulong)largest;
        int shift = 2;
        for (int i = 0; i < 4; i++)
        {
            if (i == largest)
                continue;

            packed |= Quantize(components[i] * sign, -SmallestThreeLimit, 2f * SmallestThreeLimit) << shift;
            shift += BitsPerValue;
        }
        return packed;
    }

    private static Quaternion UnpackRotation(ulong packed)
    {
        int largest = (int)(packed & 3);
        float[] components = new float[4];
        float sumOfSquares = 0f;
        int shift = 2;
        for (int i = 0; i < 4; i++)
        {
            if (i == largest)
                continue;

            components[i] = Dequantize(packed >> shift & ValueMask, -SmallestThreeLimit, 2f * SmallestThreeLimit);
            sumOfSquares += components[i] * components[i];
            shift += BitsPerValue;
        }
        components[largest] = Mathf.Sqrt(Mathf.Max(0f, 1f - sumOfSquares));
        return new Quaternion(components[0], components[1], components[2], components[3]);
    }

    private static ulong Quantize(float value, float min, float range)
    {
        if (range <= 0f)
            return 0UL;

        return (ulong)Mathf.RoundToInt(Mathf.Clamp01((value - min) / range) * ValueSteps);
    }

    private static float Dequantize(ulong value, float min, float range)
    {
        return min + value / ValueSteps * range;
    }

    private static void WriteVector(BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }

    private static Vector3 ReadVector(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
