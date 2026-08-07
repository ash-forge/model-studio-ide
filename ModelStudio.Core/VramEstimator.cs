using System;

namespace ModelStudio.Core;

public class VramEstimate
{
    public double ModelWeightsGb { get; set; }
    public double KvCache4kGb { get; set; }
    public double KvCache32kGb { get; set; }
    public double KvCache128kGb { get; set; }
    public double TotalRecommendedVramGb { get; set; }
}

public class VramEstimator
{
    public static VramEstimate EstimateVram(ulong totalParams, int layers = 40, int hiddenSize = 3840)
    {
        // Average bits per param assumption: ~4.5 bits for IQ4_XS mix
        double weightsGb = (totalParams * 4.5) / (8.0 * 1024 * 1024 * 1024);

        // KV cache calculation: 2 * layers * hiddenSize * context_length * bytes_per_element
        double kvPerTokenBytes = 2.0 * layers * hiddenSize * 2.0; // FP16 KV cache

        double kv4kGb = (kvPerTokenBytes * 4096) / (1024 * 1024 * 1024);
        double kv32kGb = (kvPerTokenBytes * 32768) / (1024 * 1024 * 1024);
        double kv128kGb = (kvPerTokenBytes * 131072) / (1024 * 1024 * 1024);

        double total = weightsGb + kv32kGb + 1.5; // + 1.5 GB CUDA/TPU overhead

        return new VramEstimate
        {
            ModelWeightsGb = Math.Round(weightsGb, 2),
            KvCache4kGb = Math.Round(kv4kGb, 2),
            KvCache32kGb = Math.Round(kv32kGb, 2),
            KvCache128kGb = Math.Round(kv128kGb, 2),
            TotalRecommendedVramGb = Math.Round(total, 2)
        };
    }
}
