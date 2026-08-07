using System;
using System.IO;

namespace ModelStudio.Core;

public class UniversalModelReader
{
    public static GgufHeader ParseHeader(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        if (ext == ".safetensors")
        {
            return SafeTensorsReader.ParseHeader(filePath);
        }

        // Default to GGUF reader (auto-validates 0x46554747 magic header)
        return GgufReader.ParseHeader(filePath);
    }
}
