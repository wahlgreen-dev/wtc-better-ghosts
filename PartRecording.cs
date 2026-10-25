using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace BetterGhosts;

public class PartRecording
{
    const string Magic = "BGH";
    const byte FormatVersion = 1;

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

        for (int sample = 0; sample < SampleCount; sample++)
        {
            for (int part = 0; part < PartPaths.Length; part++)
            {
                var position = Positions[sample][part];
                var rotation = Rotations[sample][part];
                writer.Write(position.x);
                writer.Write(position.y);
                writer.Write(position.z);
                writer.Write(rotation.x);
                writer.Write(rotation.y);
                writer.Write(rotation.z);
                writer.Write(rotation.w);
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

        var recording = new PartRecording(vehicleName, partPaths);
        for (int sample = 0; sample < sampleCount; sample++)
        {
            var positions = new Vector3[partPaths.Length];
            var rotations = new Quaternion[partPaths.Length];
            for (int part = 0; part < partPaths.Length; part++)
            {
                positions[part] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
                rotations[part] = new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
            }
            recording.Add(positions, rotations);
        }
        return recording;
    }
}
