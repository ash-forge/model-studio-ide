using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace ModelStudio.Core;

public enum GgufValueType : uint
{
    Uint8 = 0,
    Int8 = 1,
    Uint16 = 2,
    Int16 = 3,
    Uint32 = 4,
    Int32 = 5,
    Float32 = 6,
    Bool = 7,
    String = 8,
    Array = 9,
    Uint64 = 10,
    Int64 = 11,
    Float64 = 12
}

public enum GgmlType : uint
{
    F32 = 0,
    F16 = 1,
    Q4_0 = 2,
    Q4_1 = 3,
    Q5_0 = 6,
    Q5_1 = 7,
    Q8_0 = 8,
    Q8_1 = 9,
    Q2_K = 10,
    Q3_K = 11,
    Q4_K = 12,
    Q5_K = 13,
    Q6_K = 14,
    Q8_K = 15,
    IQ2_XXS = 16,
    IQ2_XS = 17,
    IQ3_XXS = 18,
    IQ1_S = 19,
    IQ4_NL = 20,
    IQ3_S = 21,
    IQ2_S = 22,
    IQ4_XS = 23,
    IQ1_M = 24,
    BF16 = 30
}

public class GgufHeader
{
    public uint Magic { get; set; }
    public uint Version { get; set; }
    public ulong TensorCount { get; set; }
    public ulong MetadataCount { get; set; }
    public Dictionary<string, object> Metadata { get; } = new(StringComparer.OrdinalIgnoreCase);
    public List<GgufTensorInfo> Tensors { get; } = new();
}

public class GgufTensorInfo
{
    public string Name { get; set; } = string.Empty;
    public ulong[] Dimensions { get; set; } = Array.Empty<ulong>();
    public GgmlType Type { get; set; }
    public ulong Offset { get; set; }

    public ulong TotalElements
    {
        get
        {
            if (Dimensions.Length == 0) return 0;
            ulong count = 1;
            foreach (var dim in Dimensions) count *= dim;
            return count;
        }
    }

    public string DimensionString => string.Join(" × ", Dimensions);

    public ulong EstimatedSizeBytes
    {
        get
        {
            double bits = Type == GgmlType.F32 ? 32.0 :
                         Type == GgmlType.F16 || Type == GgmlType.BF16 ? 16.0 :
                         Type == GgmlType.Q8_0 || Type == GgmlType.Q8_K ? 8.5 :
                         Type == GgmlType.Q6_K ? 6.56 :
                         Type == GgmlType.Q4_K || Type == GgmlType.Q4_0 ? 4.5 :
                         Type == GgmlType.IQ4_XS ? 4.25 : 4.5;
            return (ulong)((TotalElements * bits) / 8.0);
        }
    }
}

public class GgufReader
{
    public static GgufHeader ParseHeader(string filePath)
    {
        var fi = new FileInfo(filePath);
        if (!fi.Exists) throw new FileNotFoundException($"GGUF file not found: {filePath}");

        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fs, Encoding.UTF8, leaveOpen: true);

        var header = new GgufHeader();
        header.Magic = reader.ReadUInt32();
        if (header.Magic != 0x46554747) // 'GGUF'
        {
            throw new InvalidDataException($"Invalid GGUF magic number 0x{header.Magic:X8}. Expected 0x46554747 (GGUF).");
        }

        header.Version = reader.ReadUInt32();
        header.TensorCount = reader.ReadUInt64();
        header.MetadataCount = reader.ReadUInt64();

        // 1. Parse Key-Value Metadata Pairs
        for (ulong i = 0; i < header.MetadataCount; i++)
        {
            var key = ReadGgufString(reader);
            var valType = (GgufValueType)reader.ReadUInt32();
            var val = ReadGgufValue(reader, valType);
            header.Metadata[key] = val;
        }

        // 2. Parse Tensor Information Records
        for (ulong i = 0; i < header.TensorCount; i++)
        {
            var tensor = new GgufTensorInfo();
            tensor.Name = ReadGgufString(reader);
            var nDimensions = reader.ReadUInt32();
            tensor.Dimensions = new ulong[nDimensions];
            for (uint d = 0; d < nDimensions; d++)
            {
                tensor.Dimensions[d] = reader.ReadUInt64();
            }
            tensor.Type = (GgmlType)reader.ReadUInt32();
            tensor.Offset = reader.ReadUInt64();
            header.Tensors.Add(tensor);
        }

        return header;
    }

    private static string ReadGgufString(BinaryReader reader)
    {
        var len = reader.ReadUInt64();
        if (len > 1024 * 1024) throw new InvalidDataException($"GGUF string length too large: {len}");
        var bytes = reader.ReadBytes((int)len);
        return Encoding.UTF8.GetString(bytes);
    }

    private static object ReadGgufValue(BinaryReader reader, GgufValueType type)
    {
        return type switch
        {
            GgufValueType.Uint8 => reader.ReadByte(),
            GgufValueType.Int8 => reader.ReadSByte(),
            GgufValueType.Uint16 => reader.ReadUInt16(),
            GgufValueType.Int16 => reader.ReadInt16(),
            GgufValueType.Uint32 => reader.ReadUInt32(),
            GgufValueType.Int32 => reader.ReadInt32(),
            GgufValueType.Float32 => reader.ReadSingle(),
            GgufValueType.Bool => reader.ReadByte() != 0,
            GgufValueType.String => ReadGgufString(reader),
            GgufValueType.Uint64 => reader.ReadUInt64(),
            GgufValueType.Int64 => reader.ReadInt64(),
            GgufValueType.Float64 => reader.ReadDouble(),
            GgufValueType.Array => ReadGgufArray(reader),
            _ => throw new NotSupportedException($"GGUF value type {type} is not supported.")
        };
    }

    private static object ReadGgufArray(BinaryReader reader)
    {
        var elemType = (GgufValueType)reader.ReadUInt32();
        var len = reader.ReadUInt64();
        var list = new List<object>();
        for (ulong i = 0; i < len; i++)
        {
            list.Add(ReadGgufValue(reader, elemType));
        }
        return list;
    }
}
