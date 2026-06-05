using System.Text.Json;
using AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

namespace AutonomusCRM.Tests.EnterpriseAI;

public class MlFeatureExtractorTests
{
    [Fact]
    public void DictToWeights_parses_JsonElement_values_from_postgres_json()
    {
        using var doc = JsonDocument.Parse("""{"health":0.8,"bias":0.1}""");
        var dict = new Dictionary<string, object>
        {
            ["health"] = doc.RootElement.GetProperty("health"),
            ["bias"] = doc.RootElement.GetProperty("bias")
        };

        var (weights, bias) = MlFeatureExtractor.DictToWeights(dict);

        Assert.Equal(0.8, weights[0], 3);
        Assert.Equal(0.1, bias, 3);
    }

    [Theory]
    [InlineData("""{"f1":0.75}""", 0.75)]
    [InlineData("""{"f1":"0.42"}""", 0.42)]
    public void ToNumeric_handles_json_element_metrics(string json, double expected)
    {
        using var doc = JsonDocument.Parse(json);
        var value = doc.RootElement.GetProperty("f1");
        Assert.Equal(expected, MlFeatureExtractor.ToNumeric(value), 3);
    }
}
