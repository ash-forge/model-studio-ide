using System;
using System.IO;

namespace ModelStudio.Core;

public class GgufSplitter
{
    public static (bool success, string message) SplitGguf(string sourcePath, long maxChunkSizeMb)
    {
        if (!File.Exists(sourcePath)) return (false, "File not found.");

        long maxBytes = maxChunkSizeMb * 1024 * 1024;
        var fi = new FileInfo(sourcePath);
        if (fi.Length <= maxBytes) return (false, $"File is smaller than chunk limit ({fi.Length / (1024 * 1024)} MB <= {maxChunkSizeMb} MB).");

        int totalChunks = (int)Math.Ceiling((double)fi.Length / maxBytes);
        var dir = fi.DirectoryName ?? ".";
        var baseName = Path.GetFileNameWithoutExtension(sourcePath);

        using var src = new FileStream(sourcePath, FileMode.Open, FileAccess.Read);
        byte[] buffer = new byte[8 * 1024 * 1024];

        for (int i = 1; i <= totalChunks; i++)
        {
            var chunkPath = Path.Combine(dir, $"{baseName}-chunk-{i:D5}-of-{totalChunks:D5}.gguf");
            using var dst = new FileStream(chunkPath, FileMode.Create, FileAccess.Write);

            long written = 0;
            while (written < maxBytes && src.Position < fi.Length)
            {
                int toRead = (int)Math.Min(buffer.Length, maxBytes - written);
                int read = src.Read(buffer, 0, toRead);
                if (read <= 0) break;
                dst.Write(buffer, 0, read);
                written += read;
            }
        }

        return (true, $"Successfully split model into {totalChunks} chunks of max {maxChunkSizeMb} MB each!");
    }
}
