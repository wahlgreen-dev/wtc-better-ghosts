using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using UnityEngine;
using WidePosition = BetterGhosts.PosePacking.WidePosition;

namespace BetterGhosts;

public static class RecordingFile
{
    private const string Magic = "BGH";
    private const byte FormatVersion = 1;

    public static void Save(PartRecording recording, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        using FileStream file = File.Create(path);
        using GZipStream gzip = new GZipStream(file, System.IO.Compression.CompressionLevel.Optimal);
        using BinaryWriter writer = new BinaryWriter(gzip, Encoding.UTF8);

        writer.Write(Encoding.ASCII.GetBytes(Magic));
        writer.Write(FormatVersion);
        writer.Write(recording.VehicleName);
        writer.Write(recording.FramePath);
        writer.Write(recording.PartsRotateOnly);
        WriteNames(writer, recording.PartPaths);
        WriteNames(writer, recording.GoalObjectNames);
        writer.Write(recording.SampleCount);

        int partCount = recording.PartPaths.Length;
        int goalCount = recording.GoalObjectNames.Length;
        (Vector3 min, Vector3 range)[] partBounds = recording.PartsRotateOnly ? new (Vector3, Vector3)[partCount] : WriteBounds(writer, recording.Positions, partCount);
        (Vector3 min, Vector3 range)[] goalBounds = WriteBounds(writer, recording.GoalPositions, goalCount);
        (Vector3 min, Vector3 range) frameBounds = recording.HasFrame ? WriteBounds(writer, recording.FramePositions) : default;

        WidePosition previousFrame = default;
        for (int sample = 0; sample < recording.SampleCount; sample++)
        {
            if (recording.HasFrame)
            {
                WidePosition frame = WidePosition.Pack(recording.FramePositions[sample], frameBounds.min, frameBounds.range);
                frame.WriteChange(writer, previousFrame);
                previousFrame = frame;
                writer.Write(PosePacking.PackRotationOnly(recording.FrameRotations[sample]));
            }

            for (int part = 0; part < partCount; part++)
            {
                Quaternion rotation = recording.Rotations[sample][part];
                if (recording.PartsRotateOnly)
                    writer.Write(PosePacking.PackRotationOnly(rotation));
                else
                    writer.Write(PosePacking.PackPose(recording.Positions[sample][part], partBounds[part].min, partBounds[part].range, rotation));
            }

            for (int goal = 0; goal < goalCount; goal++)
                WidePosition.Pack(recording.GoalPositions[sample][goal], goalBounds[goal].min, goalBounds[goal].range).Write(writer);
        }
    }

    public static PartRecording Load(string path)
    {
        using FileStream file = File.OpenRead(path);
        using GZipStream gzip = new GZipStream(file, CompressionMode.Decompress);
        using BinaryReader reader = new BinaryReader(gzip, Encoding.UTF8);

        if (Encoding.ASCII.GetString(reader.ReadBytes(Magic.Length)) != Magic || reader.ReadByte() != FormatVersion)
            return null;

        string vehicleName = reader.ReadString();
        string framePath = reader.ReadString();
        bool partsRotateOnly = reader.ReadBoolean();
        string[] partPaths = ReadNames(reader);
        string[] goalObjectNames = ReadNames(reader);
        int sampleCount = reader.ReadInt32();
        PartRecording recording = new PartRecording(vehicleName, framePath, partsRotateOnly, partPaths, goalObjectNames);

        (Vector3 min, Vector3 range)[] partBounds = partsRotateOnly ? new (Vector3, Vector3)[partPaths.Length] : ReadBounds(reader, partPaths.Length);
        (Vector3 min, Vector3 range)[] goalBounds = ReadBounds(reader, goalObjectNames.Length);
        (Vector3 min, Vector3 range) frameBounds = recording.HasFrame ? ReadBounds(reader) : default;

        WidePosition previousFrame = default;
        for (int sample = 0; sample < sampleCount; sample++)
        {
            Vector3 framePosition = Vector3.zero;
            Quaternion frameRotation = Quaternion.identity;
            if (recording.HasFrame)
            {
                previousFrame = WidePosition.ReadChange(reader, previousFrame);
                framePosition = previousFrame.Unpack(frameBounds.min, frameBounds.range);
                frameRotation = PosePacking.UnpackRotationOnly(reader.ReadUInt32());
            }

            Vector3[] positions = new Vector3[partPaths.Length];
            Quaternion[] rotations = new Quaternion[partPaths.Length];
            for (int part = 0; part < partPaths.Length; part++)
            {
                if (partsRotateOnly)
                    rotations[part] = PosePacking.UnpackRotationOnly(reader.ReadUInt32());
                else
                    PosePacking.UnpackPose(reader.ReadUInt64(), partBounds[part].min, partBounds[part].range, out positions[part], out rotations[part]);
            }

            Vector3[] goalPositions = new Vector3[goalObjectNames.Length];
            for (int goal = 0; goal < goalObjectNames.Length; goal++)
                goalPositions[goal] = WidePosition.Read(reader).Unpack(goalBounds[goal].min, goalBounds[goal].range);
            recording.Add(framePosition, frameRotation, positions, rotations, goalPositions);
        }
        return recording;
    }

    private static void WriteNames(BinaryWriter writer, string[] names)
    {
        writer.Write(names.Length);
        foreach (string name in names)
            writer.Write(name);
    }

    private static string[] ReadNames(BinaryReader reader)
    {
        string[] names = new string[reader.ReadInt32()];
        for (int i = 0; i < names.Length; i++)
            names[i] = reader.ReadString();
        return names;
    }

    private static (Vector3 min, Vector3 range)[] WriteBounds(BinaryWriter writer, List<Vector3[]> samples, int count)
    {
        (Vector3 min, Vector3 range)[] bounds = new (Vector3, Vector3)[count];
        for (int i = 0; i < count; i++)
            bounds[i] = WriteBounds(writer, samples.Select(sample => sample[i]).ToList());
        return bounds;
    }

    private static (Vector3 min, Vector3 range) WriteBounds(BinaryWriter writer, List<Vector3> values)
    {
        Vector3 min = values[0];
        Vector3 max = min;
        for (int i = 1; i < values.Count; i++)
        {
            min = Vector3.Min(min, values[i]);
            max = Vector3.Max(max, values[i]);
        }

        WriteVector(writer, min);
        WriteVector(writer, max - min);
        return (min, max - min);
    }

    private static (Vector3 min, Vector3 range)[] ReadBounds(BinaryReader reader, int count)
    {
        (Vector3 min, Vector3 range)[] bounds = new (Vector3, Vector3)[count];
        for (int i = 0; i < count; i++)
            bounds[i] = ReadBounds(reader);
        return bounds;
    }

    private static (Vector3 min, Vector3 range) ReadBounds(BinaryReader reader)
    {
        return (ReadVector(reader), ReadVector(reader));
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
