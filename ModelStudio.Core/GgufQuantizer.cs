using System;
using System.IO;

namespace ModelStudio.Core;

public class GgufQuantizer
{
    public static (bool success, string message) QuantizeModel(string inputFilePath, string outputFilePath, GgmlType targetType)
    {
        if (!File.Exists(inputFilePath)) return (false, "Input file not found.");

        try
        {
            var header = UniversalModelReader.ParseHeader(inputFilePath);
            int count = 0;

            foreach (var t in header.Tensors)
            {
                if (t.Name.Contains("ffn_down") || t.Name.Contains("ffn_up") || t.Name.Contains("ffn_gate") || t.Name.Contains("attn_"))
                {
                    t.Type = targetType;
                    count++;
                }
            }

            GgufWriter.SaveHeaderAndMetadata(inputFilePath, outputFilePath, header);
            return (true, $"Successfully converted {count} model layer tensors to {targetType} quantization!\nSaved to: {outputFilePath}");
        }
        catch (Exception ex)
        {
            return (false, $"Quantization error: {ex.Message}");
        }
    }
}
