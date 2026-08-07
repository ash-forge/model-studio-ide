using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ModelStudio.Core;

public class LoraAdapterInfo
{
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int Rank { get; set; } = 8;
    public float Alpha { get; set; } = 16.0f;
    public float Scale => Rank > 0 ? Alpha / Rank : 1.0f;
    public List<string> TargetModules { get; } = new();
}

public class LoraEngine
{
    public static LoraAdapterInfo ParseLoraAdapter(string filePath)
    {
        var info = new LoraAdapterInfo
        {
            Name = Path.GetFileNameWithoutExtension(filePath),
            FilePath = filePath
        };

        try
        {
            var header = UniversalModelReader.ParseHeader(filePath);
            foreach (var t in header.Tensors)
            {
                if (t.Name.Contains("lora_a") || t.Name.Contains("lora_b"))
                {
                    var modName = t.Name.Replace(".lora_a.weight", "").Replace(".lora_b.weight", "");
                    if (!info.TargetModules.Contains(modName))
                    {
                        info.TargetModules.Add(modName);
                    }
                }
            }

            if (header.Metadata.TryGetValue("adapter.rank", out var r)) info.Rank = Convert.ToInt32(r);
            if (header.Metadata.TryGetValue("adapter.alpha", out var a)) info.Alpha = Convert.ToSingle(a);
        }
        catch { }

        return info;
    }
}
