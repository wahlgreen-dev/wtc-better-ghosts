using System.IO;
using UnityEngine;

namespace BetterGhosts;

public static class PosePacking
{
    private const int BitsPerValue = 10;
    private const int PositionBits = 3 * BitsPerValue;
    private const ulong ValueMask = (1UL << BitsPerValue) - 1;
    private const float ValueSteps = ValueMask;
    private const float WideSteps = ushort.MaxValue;
    private const float SmallestThreeLimit = 0.70710678f;

    public static ulong PackPose(Vector3 position, Vector3 min, Vector3 range, Quaternion rotation)
    {
        return PackPosition(position, min, range) | PackRotation(rotation) << PositionBits;
    }

    public static void UnpackPose(ulong packed, Vector3 min, Vector3 range, out Vector3 position, out Quaternion rotation)
    {
        position = UnpackPosition(packed, min, range);
        rotation = UnpackRotation(packed >> PositionBits);
    }

    public static uint PackRotationOnly(Quaternion rotation)
    {
        return (uint)PackRotation(rotation);
    }

    public static Quaternion UnpackRotationOnly(uint packed)
    {
        return UnpackRotation(packed);
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

    public struct WidePosition
    {
        private ushort x;
        private ushort y;
        private ushort z;

        public static WidePosition Pack(Vector3 position, Vector3 min, Vector3 range)
        {
            return new WidePosition
            {
                x = (ushort)Quantize(position.x, min.x, range.x, WideSteps),
                y = (ushort)Quantize(position.y, min.y, range.y, WideSteps),
                z = (ushort)Quantize(position.z, min.z, range.z, WideSteps),
            };
        }

        public Vector3 Unpack(Vector3 min, Vector3 range)
        {
            return new Vector3(Dequantize(x, min.x, range.x, WideSteps), Dequantize(y, min.y, range.y, WideSteps), Dequantize(z, min.z, range.z, WideSteps));
        }

        public void Write(BinaryWriter writer)
        {
            writer.Write(x);
            writer.Write(y);
            writer.Write(z);
        }

        public static WidePosition Read(BinaryReader reader)
        {
            return new WidePosition { x = reader.ReadUInt16(), y = reader.ReadUInt16(), z = reader.ReadUInt16() };
        }

        public void WriteChange(BinaryWriter writer, WidePosition previous)
        {
            writer.Write((ushort)(x - previous.x));
            writer.Write((ushort)(y - previous.y));
            writer.Write((ushort)(z - previous.z));
        }

        public static WidePosition ReadChange(BinaryReader reader, WidePosition previous)
        {
            return new WidePosition
            {
                x = (ushort)(previous.x + reader.ReadUInt16()),
                y = (ushort)(previous.y + reader.ReadUInt16()),
                z = (ushort)(previous.z + reader.ReadUInt16()),
            };
        }
    }
}
