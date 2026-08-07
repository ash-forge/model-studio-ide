using System;

namespace ModelStudio.Core;

public class QuantCalculator
{
    public static double EstimateVramGb(ulong totalElements, GgmlType type)
    {
        double bitsPerWeight = type switch
        {
            GgmlType.IQ4_XS => 4.25,
            GgmlType.Q4_0 or GgmlType.Q4_1 or GgmlType.Q4_K => 4.50,
            GgmlType.Q5_0 or GgmlType.Q5_K => 5.50,
            GgmlType.Q6_K => 6.56,
            GgmlType.Q8_0 => 8.50,
            GgmlType.F16 or GgmlType.BF16 => 16.0,
            GgmlType.F32 => 32.0,
            _ => 5.0
        };

        double bytes = (totalElements * bitsPerWeight) / 8.0;
        return bytes / (1024.0 * 1024.0 * 1024.0);
    }
}
