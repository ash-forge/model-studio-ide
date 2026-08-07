using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;

namespace ModelStudio.Core;

public class SafeTensorsHeaderInfo
{
    public string Dtype { get; set; } = string.Empty;
    public ulong[] Shape { get; set; } = Array.Empty<ulong>();
    public ulong[] DataOffsets { get; set; } = Array.Empty<ulong>();
}

public class SafeTensorsReader
{
    public static GgufHeader ParseHeader(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var reader = new BinaryReader(fs, Encoding.UTF8);

        ulong headerLen = reader.ReadUInt64();
        if (headerLen > 100 * 1024 * 1024) throw new InvalidDataException($"SafeTensors header size too large: {headerLen}");

        byte[] headerBytes = reader.ReadBytes((int)headerLen);
        string headerJson = Encoding.UTF8.GetString(headerBytes);

        using var doc = JsonDocument.Parse(headerJson);
        var header = new GgufHeader
        {
            Magic = 0x53414645, // 'SAFE'
            Version = 1,
            MetadataCount = 0,
            TensorCount = 0
        };

        header.Metadata["general.architecture"] = "safetensors";
        header.Metadata["general.name"] = Path.GetFileNameWithoutExtension(filePath);
        header.Metadata["general.file_type"] = "SafeTensors (HuggingFace)";

        foreach (var prop in doc.RootElement.EnumerateObject())
        {
            if (prop.Name == "__metadata__")
            {
                foreach (var metaProp in prop.Value.EnumerateObject())
                {
                    header.Metadata[metaProp.Name] = metaProp.Value.GetString() ?? "";
                    header.MetadataCount++;
                }
                continue;
            }

            var tensor = new GgufTensorInfo { Name = prop.Name };
            var shapeList = new List<ulong>();
            if (prop.Value.TryGetProperty("shape", out var shapeElem))
            {
                foreach (var dim in shapeElem.EnumerateArray())
                {
                    shapeList.Add(dim.GetUInt64());
                }
            }
            tensor.Dimensions = shapeList.ToArray();

            var dtype = prop.Value.TryGetProperty("dtype", out var dtElem) ? dtElem.GetString() ?? "F32" : "F32";
            tensor.Type = MapDtypeToGgmlType(dtype);

            if (prop.Value.TryGetProperty("data_offsets", out var offElem) && offElem.GetArrayLength() >= 2)
            {
                tensor.Offset = offElem[0].GetUInt64();
            }

            header.Tensors.Add(tensor);
            header.TensorCount++;
        }

        return header;
    }

    private static GgmlType MapDtypeToGgmlType(string dtype) => dtype.ToUpperInvariant() switch
    {
        "F32" or "FLOAT32" => GgmlType.F32,
        "F16" or "FLOAT16" => GgmlType.F16,
        "BF16" or "BFLOAT16" => GgmlType.BF16,
        "I8" or "INT8" => GgmlType.Q8_0,
        "I4" or "INT4" => GgmlType.Q4_0,
        _ => GgmlType.F32
    };
}
