using System;
using System.Collections.Generic;

namespace ModelStudio.Core;

public class ModelMergePlan
{
    public string ModelAPath { get; set; } = string.Empty;
    public string ModelBPath { get; set; } = string.Empty;
    public float SlerpRatio { get; set; } = 0.5f; // 0.0 = 100% Model A, 1.0 = 100% Model B
    public string MergeMethod { get; set; } = "SLERP (Spherical Linear)";
}

public class ModelMerger
{
    public static (bool success, string summary) PreviewMerge(ModelMergePlan plan)
    {
        if (string.IsNullOrEmpty(plan.ModelAPath) || string.IsNullOrEmpty(plan.ModelBPath))
        {
            return (false, "Both Model A and Model B file paths are required.");
        }

        try
        {
            var headerA = UniversalModelReader.ParseHeader(plan.ModelAPath);
            var headerB = UniversalModelReader.ParseHeader(plan.ModelBPath);

            var summary = $"Merge Plan Ready:\n" +
                          $"  • Model A: {headerA.Metadata.GetValueOrDefault("general.name", "ModelA")} ({headerA.TensorCount:N0} tensors)\n" +
                          $"  • Model B: {headerB.Metadata.GetValueOrDefault("general.name", "ModelB")} ({headerB.TensorCount:N0} tensors)\n" +
                          $"  • Merge Method: {plan.MergeMethod}\n" +
                          $"  • SLERP Interpolation Ratio: {plan.SlerpRatio:F2} (Model A: {(1.0f - plan.SlerpRatio) * 100:F0}%, Model B: {plan.SlerpRatio * 100:F0}%)\n" +
                          $"  • Target Merged Architecture: {headerA.Metadata.GetValueOrDefault("general.architecture", "gguf")}";

            return (true, summary);
        }
        catch (Exception ex)
        {
            return (false, $"Merge preview error: {ex.Message}");
        }
    }
}
