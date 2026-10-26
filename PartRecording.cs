using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace BetterGhosts;

public class PartRecording
{
    const string Magic = "BGH";
    const byte FormatVersion = 2;
    const int BitsPerValue = 10;
    const ulong ValueMask = (1UL << BitsPerValue) - 1;
    const float ValueSteps = ValueMask;
    const float SmallestThreeLimit = 0.70710678f;

    public readonly string VehicleName;
    public readonly string[] PartPaths;
    public readonly List<Vector3[]> Positions = new();
    public readonly List<Quaternion[]> Rotations = new();

    public int SampleCount => Positions.Count;

    public PartRecording(string vehicleName, string[] partPaths)
    {
        VehicleName = vehicleName;
        PartPaths = partPaths;
    }

    public void Add(Vector3[] positions, Quaternion[] rotations)
    {
        Positions.Add(positions);
        Rotations.Add(rotations);
    }

    public void Clear()
    {
        Positions.Clear();
        Rotations.Clear();
    }

    public void Save(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using var file = File.Create(path);
        using var gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal);
        using var writer = new BinaryWriter(gzip, Encoding.UTF8);

        writer.Write(Encoding.ASCII.GetBytes(Magic));
        writer.Write(FormatVersion);
        writer.Write(VehicleName);
        writer.Write(PartPaths.Length);
        foreach (var partPath in PartPaths)
            writer.Write(partPath);
        writer.Write(SampleCount);

        var mins = new Vector3[PartPaths.Length];
        var ranges = new Vector3[PartPaths.Length];
        for (int part = 0; part < PartPaths.Length; part++)
        {
            (mins[part], ranges[part]) = PositionBounds(part);
            WriteVector(writer, mins[part]);
            WriteVector(writer, ranges[part]);
        }

        for (int sample = 0; sample < SampleCount; sample++)
        {
            for (int part = 0; part < PartPaths.Length; part++)
            {
                writer.Write(PackPosition(Positions[sample][part], mins[part], ranges[part])
                    | PackRotation(Rotations[sample][part]) << 30);
            }
        }
    }

    public static PartRecording Load(string path)
    {
        using var file = File.OpenRead(path);
        using var gzip = new GZipStream(file, CompressionMode.Decompress);
        using var reader = new BinaryReader(gzip, Encoding.UTF8);

        if (Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length)) != Magic)
            throw new InvalidDataException("not a Better Ghosts recording");
        var version = reader.ReadByte();
        if (version != FormatVersion)
            throw new InvalidDataException($"format version {version}");

        var vehicleName = reader.ReadString();
        var partPaths = new string[reader.ReadInt32()];
        for (int part = 0; part < partPaths.Length; part++)
            partPaths[part] = reader.ReadString();
        var sampleCount = reader.ReadInt32();

        var mins = new Vector3[partPaths.Length];
        var ranges = new Vector3[partPaths.Length];
        for (int part = 0; part < partPaths.Length; part++)
        {
            mins[part] = ReadVector(reader);
            ranges[part] = ReadVector(reader);
        }

        var recording = new PartRecording(vehicleName, partPaths);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            var positions = new Vector3[partPaths.Length];
            var rotations = new Quaternion[partPaths.Length];
            for (int part = 0; part < partPaths.Length; part++)
            {
                var packed = reader.ReadUInt64();
                positions[part] = UnpackPosition(packed, mins[part], ranges[part]);
                rotations[part] = UnpackRotation(packed >> 30);
            }
            recording.Add(positions, rotations);
        }
        return recording;
    }

    (Vector3 min, Vector3 range) PositionBounds(int part)
    {
        if (SampleCount == 0)
            return (Vector3.zero, Vector3.zero);

        var min = Positions[0][part];
        var max = min;
        for (int sample = 1; sample < SampleCount; sample++)
        {
            min = Vector3.Min(min, Positions[sample][part]);
            max = Vector3.Max(max, Positions[sample][part]);
        }
        return (min, max - min);
    }

    static ulong PackPosition(Vector3 position, Vector3 min, Vector3 range)
    {
        return Quantize(position.x, min.x, range.x)
            | Quantize(position.y, min.y, range.y) << BitsPerValue
            | Quantize(position.z, min.z, range.z) << (2 * BitsPerValue);
    }

    static Vector3 UnpackPosition(ulong packed, Vector3 min, Vector3 range)
    {
        return new Vector3(
            Dequantize(packed & ValueMask, min.x, range.x),
            Dequantize(packed >> BitsPerValue & ValueMask, min.y, range.y),
            Dequantize(packed >> (2 * BitsPerValue) & ValueMask, min.z, range.z));
    }

    static ulong PackRotation(Quaternion rotation)
    {
        var components = new[] { rotation.x, rotation.y, rotation.z, rotation.w };
        var largest = 0;
        for (int i = 1; i < 4; i++)
        {
            if (Mathf.Abs(components[i]) > Mathf.Abs(components[largest]))
                largest = i;
        }

        var sign = components[largest] < 0f ? -1f : 1f;
        ulong packed = (ulong)largest;
        var shift = 2;
        for (int i = 0; i < 4; i++)
        {
            if (i == largest)
                continue;
            packed |= Quantize(components[i] * sign, -SmallestThreeLimit, 2f * SmallestThreeLimit) << shift;
            shift += BitsPerValue;
        }
        return packed;
    }

    static Quaternion UnpackRotation(ulong packed)
    {
        var largest = (int)(packed & 3);
        var components = new float[4];
        var sumOfSquares = 0f;
        var shift = 2;
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

    static ulong Quantize(float value, float min, float range)
    {
        return range > 0f ? (ulong)Mathf.RoundToInt(Mathf.Clamp01((value - min) / range) * ValueSteps) : 0UL;
    }

    static float Dequantize(ulong value, float min, float range)
    {
        return min + value / ValueSteps * range;
    }

    static void WriteVector(BinaryWriter writer, Vector3 vector)
    {
        writer.Write(vector.x);
        writer.Write(vector.y);
        writer.Write(vector.z);
    }

    static Vector3 ReadVector(BinaryReader reader)
    {
        return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
    }
}
