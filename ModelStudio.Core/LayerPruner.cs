using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelStudio.Core;

public class LayerPruner
{
    public static (int prunedCount, ulong paramsSaved) PruneLayerBlock(GgufHeader header, int layerIndex)
    {
        var targetPrefix = $"blk.{layerIndex}.";
        var toRemove = header.Tensors.Where(t => t.Name.StartsWith(targetPrefix, StringComparison.OrdinalIgnoreCase)).ToList();

        ulong paramsSaved = 0;
        foreach (var t in toRemove)
        {
            paramsSaved += t.TotalElements;
            header.Tensors.Remove(t);
        }

        header.TensorCount = (ulong)header.Tensors.Count;
        return (toRemove.Count, paramsSaved);
    }
}
