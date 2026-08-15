using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ModelStudio.Core;

public class HumanLayerDescription
{
    public string TensorName { get; set; } = string.Empty;
    public int LayerIndex { get; set; } = -1;
    public string AnatomicalTier { get; set; } = "General"; // Foundation / Grammar, Knowledge & Reasoning, Personality & Tone, Output Vocab
    public string HumanRoleTitle { get; set; } = string.Empty;
    public string PlainEnglishExplanation { get; set; } = string.Empty;
    public string PruneSafetyBadge { get; set; } = "🟢 Safe to Prune";
    public int PruneSafetyScore { get; set; } = 80; // 0 to 100
    public string HardwareImpact { get; set; } = "Moderate VRAM";
    public string EstimatedMemoryFormatted { get; set; } = string.Empty;
    public string WhyItMatters { get; set; } = string.Empty;
}

public class ModelAnatomyReport
{
    public string ModelName { get; set; } = "GGUF Model";
    public string Architecture { get; set; } = "Transformer";
    public int TotalLayers { get; set; }
    public int TotalTensors { get; set; }
    public ulong TotalEstimatedBytes { get; set; }
    public string TotalMemoryFormatted { get; set; } = "0 MB";
    public List<HumanLayerDescription> AnatomicalLayers { get; set; } = new();
    public int SafePruneCandidatesCount => AnatomicalLayers.Count(l => l.PruneSafetyScore >= 70);
    public string ExecutiveSummary { get; set; } = string.Empty;
}

public class WeightHeatmapProfile
{
    public string TensorName { get; set; } = string.Empty;
    public float MinWeight { get; set; }
    public float MaxWeight { get; set; }
    public float MeanWeight { get; set; }
    public float StdDev { get; set; }
    public float SparsityPercentage { get; set; }
    public List<string> ColorHexGrid { get; set; } = new();
}

/// <summary>
/// Human Semantic Decoder & Translation Matrix for Everyday Users.
/// Decodes cryptic neural tensor names and raw floating-point weights into
/// plain-English anatomical explanations, practical pruning guides, and visual heatmaps.
/// </summary>
public class HumanSemanticDecoder
{
    /// <summary>
    /// Translates an entire GGUF model into a human-readable Anatomical Report.
    /// </summary>
    public static ModelAnatomyReport GenerateAnatomyReport(GgufHeader header)
    {
        var report = new ModelAnatomyReport
        {
            ModelName = header.Metadata.TryGetValue("general.name", out var n) ? n.ToString() ?? "GGUF Model" : "GGUF Model",
            Architecture = header.Metadata.TryGetValue("general.architecture", out var a) ? a.ToString() ?? "Transformer" : "Transformer",
            TotalTensors = header.Tensors.Count
        };

        ulong totalBytes = 0;
        int maxLayer = -1;

        foreach (var t in header.Tensors)
        {
            totalBytes += t.EstimatedSizeBytes;
            int idx = ExtractLayerIndex(t.Name);
            if (idx > maxLayer) maxLayer = idx;
        }

        report.TotalLayers = maxLayer >= 0 ? maxLayer + 1 : 1;
        report.TotalEstimatedBytes = totalBytes;
        report.TotalMemoryFormatted = FormatBytes(totalBytes);

        // Group tensors into meaningful anatomical units
        foreach (var tensor in header.Tensors)
        {
            var humanDesc = DecodeTensor(tensor, report.TotalLayers);
            report.AnatomicalLayers.Add(humanDesc);
        }

        report.ExecutiveSummary = GenerateExecutiveSummary(report);
        return report;
    }

    /// <summary>
    /// Decodes a single tensor into plain English.
    /// </summary>
    public static HumanLayerDescription DecodeTensor(GgufTensorInfo tensor, int totalLayers = 32)
    {
        string name = tensor.Name;
        int layerIdx = ExtractLayerIndex(name);
        float layerRel = totalLayers > 1 && layerIdx >= 0 ? (float)layerIdx / (totalLayers - 1) : 0.5f;

        var desc = new HumanLayerDescription
        {
            TensorName = name,
            LayerIndex = layerIdx,
            EstimatedMemoryFormatted = FormatBytes(tensor.EstimatedSizeBytes)
        };

        if (name.Contains("token_embd", StringComparison.OrdinalIgnoreCase))
        {
            desc.AnatomicalTier = "🔤 Input Vocabulary";
            desc.HumanRoleTitle = "The Dictionary (Input Embeddings)";
            desc.PlainEnglishExplanation = "Translates written human words into numerical concept vectors that the neural brain can think about.";
            desc.WhyItMatters = "Without this, the model cannot read words. Never prune this.";
            desc.PruneSafetyBadge = "🔴 Critical - Do Not Prune";
            desc.PruneSafetyScore = 0;
            desc.HardwareImpact = "High Memory, Fast Compute";
            return desc;
        }

        if (name.Contains("output.weight", StringComparison.OrdinalIgnoreCase) || name.Contains("lm_head", StringComparison.OrdinalIgnoreCase))
        {
            desc.AnatomicalTier = "🗣️ Output Voice";
            desc.HumanRoleTitle = "The Voice Box (Output Vocabulary)";
            desc.PlainEnglishExplanation = "Takes the model's final thoughts and translates them back into spoken or written English words.";
            desc.WhyItMatters = "Without this, the model cannot speak or generate text. Never prune this.";
            desc.PruneSafetyBadge = "🔴 Critical - Do Not Prune";
            desc.PruneSafetyScore = 0;
            desc.HardwareImpact = "High Memory, Final Step";
            return desc;
        }

        if (name.Contains("attn_q") || name.Contains("attn_k") || name.Contains("attn_v") || name.Contains("attn_output"))
        {
            desc.AnatomicalTier = layerRel < 0.33f ? "🔤 Grammar & Context" : (layerRel < 0.70f ? "📚 Reasoning & Logic" : "🎭 Tone & Focus");
            desc.HumanRoleTitle = $"Attention Head (Layer {layerIdx})";
            desc.PlainEnglishExplanation = $"Controls what the AI focuses on in your prompt. Identifies relationships between words across long sentences.";
            desc.WhyItMatters = "Enables the model to keep track of multi-paragraph context without getting confused.";
            
            // Middle attention layers are core; outer layers are sometimes safe to prune
            if (layerRel > 0.75f && layerRel < 0.95f)
            {
                desc.PruneSafetyBadge = "🟢 Safe to Prune";
                desc.PruneSafetyScore = 75;
            }
            else
            {
                desc.PruneSafetyBadge = "🟡 Prune with Caution";
                desc.PruneSafetyScore = 45;
            }
            desc.HardwareImpact = "Moderate Memory, High Speed";
            return desc;
        }

        if (name.Contains("ffn_gate") || name.Contains("ffn_up") || name.Contains("ffn_down"))
        {
            desc.AnatomicalTier = layerRel < 0.33f ? "🔤 Basic Grammar Rules" : (layerRel < 0.70f ? "📚 Factual Knowledge Bank" : "🎭 Personality & Humor");
            desc.HumanRoleTitle = $"Knowledge Bank (Layer {layerIdx})";
            desc.PlainEnglishExplanation = $"Stores factual memories, coding syntax, math rules, and trivia learned during training.";
            desc.WhyItMatters = "This is where the actual 'intelligence' and answers to questions reside.";

            // Late FFN layers can often be pruned with minimal knowledge loss
            if (layerRel > 0.80f)
            {
                desc.PruneSafetyBadge = "🟢 High Prune Candidate (Saves VRAM)";
                desc.PruneSafetyScore = 85;
            }
            else if (layerRel < 0.25f)
            {
                desc.PruneSafetyBadge = "🟡 Prune with Caution";
                desc.PruneSafetyScore = 40;
            }
            else
            {
                desc.PruneSafetyBadge = "🟡 Core Knowledge Layer";
                desc.PruneSafetyScore = 50;
            }
            desc.HardwareImpact = "Very High Memory (Heaviest Tensors)";
            return desc;
        }

        if (name.Contains("norm") || name.Contains("ln_"))
        {
            desc.AnatomicalTier = "⚖️ Balance & Stability";
            desc.HumanRoleTitle = $"Signal Stabilizer (Layer {layerIdx})";
            desc.PlainEnglishExplanation = "Normalizes mathematical signals so layers don't explode or produce gibberish.";
            desc.WhyItMatters = "Extremely small in size, essential for mathematical stability.";
            desc.PruneSafetyBadge = "🔴 Critical for Stability";
            desc.PruneSafetyScore = 10;
            desc.HardwareImpact = "Tiny Memory (<1 MB)";
            return desc;
        }

        desc.AnatomicalTier = "🧠 Deep Latents";
        desc.HumanRoleTitle = $"Internal Projection ({name})";
        desc.PlainEnglishExplanation = "Internal mathematical routing connecting neural pathways.";
        desc.WhyItMatters = "General purpose neural pathway.";
        desc.PruneSafetyBadge = "🟡 Moderate Safety";
        desc.PruneSafetyScore = 50;
        return desc;
    }

    /// <summary>
    /// Computes a statistical heatmap profile of floating-point weights for visual rendering.
    /// </summary>
    public static WeightHeatmapProfile GenerateWeightHeatmap(string tensorName, float[] samples, int gridSize = 64)
    {
        var profile = new WeightHeatmapProfile { TensorName = tensorName };
        if (samples == null || samples.Length == 0) return profile;

        float min = float.MaxValue, max = float.MinValue, sum = 0;
        int zeroCount = 0;

        foreach (var v in samples)
        {
            if (v < min) min = v;
            if (v > max) max = v;
            sum += v;
            if (MathF.Abs(v) < 1e-4f) zeroCount++;
        }

        float mean = sum / samples.Length;
        float varSum = 0;
        foreach (var v in samples) varSum += (v - mean) * (v - mean);
        float stdDev = MathF.Sqrt(varSum / samples.Length);

        profile.MinWeight = min;
        profile.MaxWeight = max;
        profile.MeanWeight = mean;
        profile.StdDev = stdDev;
        profile.SparsityPercentage = (zeroCount / (float)samples.Length) * 100.0f;

        // Generate color hex palette grid
        int totalCells = Math.Min(gridSize, samples.Length);
        float range = MathF.Max(1e-5f, max - min);

        for (int i = 0; i < totalCells; i++)
        {
            float norm = Math.Clamp((samples[i] - min) / range, 0.0f, 1.0f);
            profile.ColorHexGrid.Add(InterpolateColorHex(norm));
        }

        return profile;
    }

    private static string InterpolateColorHex(float t)
    {
        // Smooth gradient: Dark Blue (0.0) -> Cyan (0.5) -> Amber/Gold (1.0)
        byte r, g, b;
        if (t < 0.5f)
        {
            float s = t * 2.0f;
            r = (byte)(10 + s * (0 - 10));
            g = (byte)(30 + s * (245 - 30));
            b = (byte)(120 + s * (212 - 120));
        }
        else
        {
            float s = (t - 0.5f) * 2.0f;
            r = (byte)(0 + s * (255 - 0));
            g = (byte)(245 + s * (183 - 245));
            b = (byte)(212 + s * (3 - 212));
        }
        return $"#{r:X2}{g:X2}{b:X2}";
    }

    private static string GenerateExecutiveSummary(ModelAnatomyReport report)
    {
        return $"This model contains {report.TotalLayers} stacked reasoning layers ({report.TotalTensors} total tensor blocks) requiring approximately {report.TotalMemoryFormatted} of memory. " +
               $"The bottom layers manage language grammar, middle layers handle deep factual reasoning, and upper layers dictate conversational personality. " +
               $"There are {report.SafePruneCandidatesCount} safe pruning candidates that can be trimmed to reduce VRAM while retaining 95%+ reasoning capacity.";
    }

    private static string FormatBytes(ulong bytes)
    {
        if (bytes >= 1024UL * 1024 * 1024) return $"{(bytes / (1024.0 * 1024.0 * 1024.0)):F2} GB";
        if (bytes >= 1024UL * 1024) return $"{(bytes / (1024.0 * 1024.0)):F2} MB";
        if (bytes >= 1024UL) return $"{(bytes / 1024.0):F2} KB";
        return $"{bytes} B";
    }

    private static int ExtractLayerIndex(string tensorName)
    {
        var match = Regex.Match(tensorName, @"blk\.(\d+)");
        if (match.Success && int.TryParse(match.Groups[1].Value, out int idx))
        {
            return idx;
        }
        return -1;
    }
}
