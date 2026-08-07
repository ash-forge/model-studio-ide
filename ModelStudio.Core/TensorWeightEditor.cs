using System;
using System.IO;
using System.IO.MemoryMappedFiles;

namespace ModelStudio.Core;

public class TensorWeightSample
{
    public ulong Index { get; set; }
    public float Value { get; set; }
}

public class TensorWeightEditor
{
    public static TensorWeightSample[] ReadTensorSampleValues(string filePath, GgufTensorInfo tensor, int count = 100)
    {
        if (!File.Exists(filePath) || tensor.Offset == 0) return Array.Empty<TensorWeightSample>();

        try
        {
            using var mmf = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
            using var accessor = mmf.CreateViewAccessor((long)tensor.Offset, Math.Min((long)tensor.TotalElements * 4, 1024 * 1024), MemoryMappedFileAccess.Read);

            int sampleCount = (int)Math.Min((ulong)count, tensor.TotalElements);
            var samples = new TensorWeightSample[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float val = 0.0f;
                try
                {
                    val = tensor.Type switch
                    {
                        GgmlType.F32 => accessor.ReadSingle(i * 4),
                        GgmlType.F16 => (float)BitConverter.UInt16BitsToHalf(accessor.ReadUInt16(i * 2)),
                        _ => (float)(accessor.ReadByte(i) - 128) / 128.0f // Normalized representation for quantized types
                    };
                }
                catch { }

                samples[i] = new TensorWeightSample { Index = (ulong)i, Value = val };
            }

            return samples;
        }
        catch
        {
            return Array.Empty<TensorWeightSample>();
        }
    }
}
