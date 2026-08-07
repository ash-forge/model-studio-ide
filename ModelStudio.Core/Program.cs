using System;
using System.IO;
using System.Linq;

namespace ModelStudio.Core;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("=========================================================");
        Console.WriteLine(" 🦙 ModelStudio IDE Core Engine v1.0.0");
        Console.WriteLine(" High-Performance GGUF & Tensor Architecture Inspector");
        Console.WriteLine("=========================================================\n");

        string? targetFile = args.Length > 0 ? args[0] : null;

        if (string.IsNullOrEmpty(targetFile))
        {
            Console.WriteLine("Usage: ModelStudio.Core <path-to-model.gguf>");
            Console.WriteLine("Or launch the ModelStudio IDE GUI application to open model files visually.\n");
            return;
        }

        if (string.IsNullOrEmpty(targetFile) || !File.Exists(targetFile))
        {
            Console.WriteLine("⚠️ No GGUF model files found. Usage: dotnet run -- <path-to-file.gguf>");
            return;
        }

        Console.WriteLine($"🔍 Loading GGUF Model: {targetFile}");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            var header = GgufReader.ParseHeader(targetFile);
            sw.Stop();

            Console.WriteLine($"✅ Successfully parsed header in {sw.ElapsedMilliseconds} ms!");
            Console.WriteLine($"---------------------------------------------------------");
            Console.WriteLine($" GGUF Magic   : 0x{header.Magic:X8} (GGUF v{header.Version})");
            Console.WriteLine($" Tensor Count : {header.TensorCount:N0} tensors");
            Console.WriteLine($" Metadata KVs : {header.MetadataCount:N0} key-value entries");
            Console.WriteLine($"---------------------------------------------------------\n");

            Console.WriteLine("📋 Key Model Metadata:");
            var interestingKeys = new[] {
                "general.architecture", "general.name", "general.file_type",
                "gemma4.context_length", "gemma2.context_length", "llama.context_length",
                "general.quantization_version", "tokenizer.ggml.model"
            };

            foreach (var key in interestingKeys)
            {
                if (header.Metadata.TryGetValue(key, out var val))
                {
                    Console.WriteLine($"  • {key,-32} : {val}");
                }
            }

            Console.WriteLine("\n📊 Layer & Tensor Breakdown (First 15 Tensors):");
            Console.WriteLine($"  {"Name",-45} | {"Dimensions",-18} | {"Quant Type",-10} | {"Elements",-12}");
            Console.WriteLine(new string('-', 95));

            foreach (var t in header.Tensors.Take(15))
            {
                Console.WriteLine($"  {t.Name,-45} | {t.DimensionString,-18} | {t.Type,-10} | {t.TotalElements,12:N0}");
            }

            if (header.Tensors.Count > 15)
            {
                Console.WriteLine($"  ... and {header.Tensors.Count - 15:N0} more tensors.");
            }

            // Summary stats
            var quantGroups = header.Tensors.GroupBy(t => t.Type).Select(g => new { Type = g.Key, Count = g.Count() });
            Console.WriteLine("\n🎛️ Tensor Quantization Formats Used:");
            foreach (var q in quantGroups)
            {
                Console.WriteLine($"  • {q.Type,-12} : {q.Count} tensors");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error parsing GGUF header: {ex.Message}");
        }
    }
}
