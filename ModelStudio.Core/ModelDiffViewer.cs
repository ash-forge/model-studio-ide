using System;
using System.Collections.Generic;
using System.Linq;

namespace ModelStudio.Core;

public class DiffItem
{
    public string Name { get; set; } = "";
    public string Category { get; set; } = "Metadata";
    public string ModelAValue { get; set; } = "";
    public string ModelBValue { get; set; } = "";
    public string Status { get; set; } = "Modified";
}

public class ModelDiffViewer
{
    public static List<DiffItem> CompareModels(GgufHeader modelA, GgufHeader modelB)
    {
        var diffs = new List<DiffItem>();

        // Compare Metadata Keys
        var allKeys = modelA.Metadata.Keys.Union(modelB.Metadata.Keys).Distinct();
        foreach (var key in allKeys)
        {
            modelA.Metadata.TryGetValue(key, out var valAObj);
            modelB.Metadata.TryGetValue(key, out var valBObj);

            string valA = valAObj?.ToString() ?? "<Missing>";
            string valB = valBObj?.ToString() ?? "<Missing>";

            if (valA != valB)
            {
                string status = valA == "<Missing>" ? "Added in B" : valB == "<Missing>" ? "Removed in B" : "Modified";
                diffs.Add(new DiffItem { Name = key, Category = "Metadata Header", ModelAValue = valA, ModelBValue = valB, Status = status });
            }
        }

        // Compare Tensors
        var dictA = modelA.Tensors.ToDictionary(t => t.Name, t => t);
        var dictB = modelB.Tensors.ToDictionary(t => t.Name, t => t);
        var allTensorNames = dictA.Keys.Union(dictB.Keys).Distinct();

        foreach (var tName in allTensorNames)
        {
            dictA.TryGetValue(tName, out var tA);
            dictB.TryGetValue(tName, out var tB);

            if (tA == null && tB != null)
            {
                diffs.Add(new DiffItem { Name = tName, Category = "Tensor Block", ModelAValue = "<Missing>", ModelBValue = $"{tB.Type} ({tB.DimensionString})", Status = "Added in B" });
            }
            else if (tA != null && tB == null)
            {
                diffs.Add(new DiffItem { Name = tName, Category = "Tensor Block", ModelAValue = $"{tA.Type} ({tA.DimensionString})", ModelBValue = "<Missing>", Status = "Removed in B" });
            }
            else if (tA != null && tB != null)
            {
                if (tA.Type != tB.Type || tA.DimensionString != tB.DimensionString)
                {
                    diffs.Add(new DiffItem { Name = tName, Category = "Tensor Block", ModelAValue = $"{tA.Type} ({tA.DimensionString})", ModelBValue = $"{tB.Type} ({tB.DimensionString})", Status = "Precision/Shape Diff" });
                }
            }
        }

        return diffs;
    }
}
