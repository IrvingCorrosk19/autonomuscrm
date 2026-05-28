using AutonomusCRM.Application.Autonomous;

namespace AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

public static class MlFeatureExtractor
{
    private static readonly string[] NumericKeys = ["health", "engagement", "adoption", "ltv", "risk", "churn_prob", "nps", "csat", "expansion_score", "renewal_days", "revenue_velocity", "deal_value"];

    public static double[] ToVector(Dictionary<string, object> features)
    {
        var vec = new double[NumericKeys.Length];
        for (var i = 0; i < NumericKeys.Length; i++)
        {
            if (features.TryGetValue(NumericKeys[i], out var v))
                vec[i] = Convert.ToDouble(v);
        }
        return Normalize(vec);
    }

    public static int LabelToBinary(string? label, string positiveLabel)
        => label != null && label.Equals(positiveLabel, StringComparison.OrdinalIgnoreCase) ? 1 : 0;

    public static (double[][] X, int[] Y) BuildMatrix(IEnumerable<MlFeatureSnapshot> samples, string positiveLabel)
    {
        var list = samples.Where(s => !string.IsNullOrEmpty(s.Label)).ToList();
        if (list.Count == 0) return ([], []);
        var x = list.Select(s => ToVector(s.Features)).ToArray();
        var y = list.Select(s => LabelToBinary(s.Label, positiveLabel)).ToArray();
        return (x, y);
    }

    private static double[] Normalize(double[] v)
    {
        var max = v.Max(Math.Abs);
        if (max < 1e-6) return v;
        return v.Select(x => x / max).ToArray();
    }

    public static Dictionary<string, object> WeightsToDict(double[] weights, double bias)
    {
        var d = new Dictionary<string, object> { ["bias"] = bias };
        for (var i = 0; i < weights.Length && i < NumericKeys.Length; i++)
            d[NumericKeys[i]] = weights[i];
        return d;
    }

    public static (double[] weights, double bias) DictToWeights(Dictionary<string, object> dict)
    {
        var weights = new double[NumericKeys.Length];
        for (var i = 0; i < NumericKeys.Length; i++)
        {
            if (dict.TryGetValue(NumericKeys[i], out var v))
                weights[i] = Convert.ToDouble(v);
        }
        var bias = dict.TryGetValue("bias", out var b) ? Convert.ToDouble(b) : 0;
        return (weights, bias);
    }
}
