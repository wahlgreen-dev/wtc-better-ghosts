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
    private const byte FormatVersionWithoutGoalObjects = 2;
    private const int BitsPerValue = 10;
    private const ulong ValueMask = (1UL << BitsPerValue) - 1;
    private const float ValueSteps = ValueMask;
    private const float GoalPositionSteps = ushort.MaxValue;
    private const float SmallestThreeLimit = 0.70710678f;

    public readonly string VehicleName;
    public readonly string[] PartPaths;
    public readonly string[] GoalObjectNames;
    public readonly List<Vector3[]> Positions = new();
    public readonly List<Quaternion[]> Rotations = new();
    public readonly List<Vector3[]> GoalPositions = new();

    public int SampleCount => Positions.Count;

    public PartRecording(string vehicleName, string[] partPaths, string[] goalObjectNames)
    {
        VehicleName = vehicleName;
        PartPaths = partPaths;
        GoalObjectNames = goalObjectNames;
    }

    public bool Matches(VehicleSetup setup)
    {
        return VehicleName == setup.VehicleName && PartPaths.SequenceEqual(setup.PartPaths);
    }

    public void Add(Vector3[] positions, Quaternion[] rotations, Vector3[] goalPositions)
    {
        Positions.Add(positions);
        Rotations.Add(rotations);
        GoalPositions.Add(goalPositions);
    }

    public void Clear()
    {
        Positions.Clear();
        Rotations.Clear();
        GoalPositions.Clear();
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
        writer.Write(GoalObjectNames.Length);
        foreach (string goalObjectName in GoalObjectNames)
            writer.Write(goalObjectName);
        writer.Write(SampleCount);

        Vector3[] mins = new Vector3[PartPaths.Length];
        Vector3[] ranges = new Vector3[PartPaths.Length];
        for (int part = 0; part < PartPaths.Length; part++)
        {
            (mins[part], ranges[part]) = Bounds(Positions, part);
            WriteVector(writer, mins[part]);
            WriteVector(writer, ranges[part]);
        }

        Vector3[] goalMins = new Vector3[GoalObjectNames.Length];
        Vector3[] goalRanges = new Vector3[GoalObjectNames.Length];
        for (int goal = 0; goal < GoalObjectNames.Length; goal++)
        {
            (goalMins[goal], goalRanges[goal]) = Bounds(GoalPositions, goal);
            WriteVector(writer, goalMins[goal]);
            WriteVector(writer, goalRanges[goal]);
        }

        for (int sample = 0; sample < SampleCount; sample++)
        {
            for (int part = 0; part < PartPaths.Length; part++)
                writer.Write(PackPosition(Positions[sample][part], mins[part], ranges[part]) | PackRotation(Rotations[sample][part]) << 30);
            for (int goal = 0; goal < GoalObjectNames.Length; goal++)
                WriteGoalPosition(writer, GoalPositions[sample][goal], goalMins[goal], goalRanges[goal]);
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
        if (version != FormatVersion && version != FormatVersionWithoutGoalObjects)
            return null;

        string vehicleName = reader.ReadString();
        string[] partPaths = new string[reader.ReadInt32()];
        for (int part = 0; part < partPaths.Length; part++)
            partPaths[part] = reader.ReadString();
        string[] goalObjectNames = new string[version == FormatVersion ? reader.ReadInt32() : 0];
        for (int goal = 0; goal < goalObjectNames.Length; goal++)
            goalObjectNames[goal] = reader.ReadString();
        int sampleCount = reader.ReadInt32();

        Vector3[] mins = new Vector3[partPaths.Length];
        Vector3[] ranges = new Vector3[partPaths.Length];
        for (int part = 0; part < partPaths.Length; part++)
        {
            mins[part] = ReadVector(reader);
            ranges[part] = ReadVector(reader);
        }

        Vector3[] goalMins = new Vector3[goalObjectNames.Length];
        Vector3[] goalRanges = new Vector3[goalObjectNames.Length];
        for (int goal = 0; goal < goalObjectNames.Length; goal++)
        {
            goalMins[goal] = ReadVector(reader);
            goalRanges[goal] = ReadVector(reader);
        }

        PartRecording recording = new PartRecording(vehicleName, partPaths, goalObjectNames);
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

            Vector3[] goalPositions = new Vector3[goalObjectNames.Length];
            for (int goal = 0; goal < goalObjectNames.Length; goal++)
                goalPositions[goal] = ReadGoalPosition(reader, goalMins[goal], goalRanges[goal]);
            recording.Add(positions, rotations, goalPositions);
        }
        return recording;
    }

    private (Vector3 min, Vector3 range) Bounds(List<Vector3[]> samples, int index)
    {
        Vector3 min = samples[0][index];
        Vector3 max = min;
        for (int sample = 1; sample < SampleCount; sample++)
        {
            min = Vector3.Min(min, samples[sample][index]);
            max = Vector3.Max(max, samples[sample][index]);
        }
        return (min, max - min);
    }

    private static void WriteGoalPosition(BinaryWriter writer, Vector3 position, Vector3 min, Vector3 range)
    {
        writer.Write((ushort)Quantize(position.x, min.x, range.x, GoalPositionSteps));
        writer.Write((ushort)Quantize(position.y, min.y, range.y, GoalPositionSteps));
        writer.Write((ushort)Quantize(position.z, min.z, range.z, GoalPositionSteps));
    }

    private static Vector3 ReadGoalPosition(BinaryReader reader, Vector3 min, Vector3 range)
    {
        float x = Dequantize(reader.ReadUInt16(), min.x, range.x, GoalPositionSteps);
        float y = Dequantize(reader.ReadUInt16(), min.y, range.y, GoalPositionSteps);
        float z = Dequantize(reader.ReadUInt16(), min.z, range.z, GoalPositionSteps);
        return new Vector3(x, y, z);
    }

    private static ulong PackPosition(Vector3 position, Vector3 min, Vector3 range)
    {
        return Quantize(position.x, min.x, range.x, ValueSteps) | Quantize(position.y, min.y, range.y, ValueSteps) << BitsPerValue | Quantize(position.z, min.z, range.z, ValueSteps) << (2 * BitsPerValue);
    }

    private static Vector3 UnpackPosition(ulong packed, Vector3 min, Vector3 range)
    {
        float x = Dequantize(packed & ValueMask, min.x, range.x, ValueSteps);
        float y = Dequantize(packed >> BitsPerValue & ValueMask, min.y, range.y, ValueSteps);
        float z = Dequantize(packed >> (2 * BitsPerValue) & ValueMask, min.z, range.z, ValueSteps);
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

            packed |= Quantize(components[i] * sign, -SmallestThreeLimit, 2f * SmallestThreeLimit, ValueSteps) << shift;
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

            components[i] = Dequantize(packed >> shift & ValueMask, -SmallestThreeLimit, 2f * SmallestThreeLimit, ValueSteps);
            sumOfSquares += components[i] * components[i];
            shift += BitsPerValue;
        }
        components[largest] = Mathf.Sqrt(Mathf.Max(0f, 1f - sumOfSquares));
        return new Quaternion(components[0], components[1], components[2], components[3]);
    }

    private static ulong Quantize(float value, float min, float range, float steps)
    {
        if (range <= 0f)
            return 0UL;

        return (ulong)Mathf.RoundToInt(Mathf.Clamp01((value - min) / range) * steps);
    }

    private static float Dequantize(ulong value, float min, float range, float steps)
    {
        return min + value / steps * range;
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
