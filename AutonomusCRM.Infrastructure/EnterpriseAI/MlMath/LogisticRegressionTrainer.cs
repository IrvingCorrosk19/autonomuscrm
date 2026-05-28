namespace AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

/// <summary>Entrenamiento logistic regression por gradiente — sin dependencias externas.</summary>
public static class LogisticRegressionTrainer
{
    public static TrainResult Train(double[][] features, int[] labels, int epochs = 200, double learningRate = 0.05)
    {
        if (features.Length == 0 || features[0].Length == 0)
            return new TrainResult([], 0, 0, 0, 0, 0);

        var n = features.Length;
        var dims = features[0].Length;
        var weights = new double[dims];
        var bias = 0.0;

        for (var epoch = 0; epoch < epochs; epoch++)
        {
            for (var i = 0; i < n; i++)
            {
                var z = bias + Dot(weights, features[i]);
                var pred = Sigmoid(z);
                var y = labels[i];
                var err = pred - y;
                for (var j = 0; j < dims; j++)
                    weights[j] -= learningRate * err * features[i][j];
                bias -= learningRate * err;
            }
        }

        var tp = 0; var fp = 0; var fn = 0; var tn = 0;
        for (var i = 0; i < n; i++)
        {
            var p = PredictProbability(weights, bias, features[i]) >= 0.5 ? 1 : 0;
            if (p == 1 && labels[i] == 1) tp++;
            else if (p == 1 && labels[i] == 0) fp++;
            else if (p == 0 && labels[i] == 1) fn++;
            else tn++;
        }

        var precision = tp + fp > 0 ? (double)tp / (tp + fp) : 0;
        var recall = tp + fn > 0 ? (double)tp / (tp + fn) : 0;
        var f1 = precision + recall > 0 ? 2 * precision * recall / (precision + recall) : 0;
        var accuracy = n > 0 ? (double)(tp + tn) / n : 0;

        return new TrainResult(weights, bias, precision, recall, f1, accuracy);
    }

    public static double PredictProbability(double[] weights, double bias, double[] features)
    {
        if (weights.Length == 0) return 0.5;
        return Sigmoid(bias + Dot(weights, features));
    }

    private static double Sigmoid(double z) => 1.0 / (1.0 + Math.Exp(-z));
    private static double Dot(double[] w, double[] x)
    {
        var sum = 0.0;
        var len = Math.Min(w.Length, x.Length);
        for (var i = 0; i < len; i++) sum += w[i] * x[i];
        return sum;
    }

    public record TrainResult(double[] Weights, double Bias, double Precision, double Recall, double F1, double Accuracy);
}
