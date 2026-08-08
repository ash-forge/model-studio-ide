using System;
using System.Collections.Generic;

namespace ModelStudio.Core;

public class LayerProfileItem
{
    public string LayerName { get; set; } = "";
    public string Type { get; set; } = "";
    public ulong TotalElements { get; set; }
    public double MemorySizeMb { get; set; }
    public double EstimatedLatencyMs { get; set; }
    public double BandwidthGbps { get; set; }
}

public class LayerProfiler
{
    public static List<LayerProfileItem> ProfileModelLayers(List<GgufTensorInfo> tensors, double hardwareBandwidthGbps = 200.0)
    {
        var list = new List<LayerProfileItem>();

        foreach (var t in tensors)
        {
            double sizeMb = t.EstimatedSizeBytes / (1024.0 * 1024.0);
            // Latency = SizeInBytes / (BandwidthInBytesPerSec)
            double latencySec = t.EstimatedSizeBytes / (hardwareBandwidthGbps * 1024.0 * 1024.0 * 1024.0);
            double latencyMs = Math.Max(0.001, latencySec * 1000.0);

            list.Add(new LayerProfileItem
            {
                LayerName = t.Name,
                Type = t.Type.ToString(),
                TotalElements = t.TotalElements,
                MemorySizeMb = Math.Round(sizeMb, 2),
                EstimatedLatencyMs = Math.Round(latencyMs, 4),
                BandwidthGbps = hardwareBandwidthGbps
            });
        }

        return list;
    }
}
