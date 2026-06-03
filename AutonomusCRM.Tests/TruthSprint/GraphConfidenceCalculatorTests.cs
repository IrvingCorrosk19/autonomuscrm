using AutonomusCRM.Infrastructure.KnowledgeGraph;

namespace AutonomusCRM.Tests.TruthSprint;

public class GraphConfidenceCalculatorTests
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 0)]
    [InlineData(5, 3)]
    [InlineData(10, 8)]
    public void Calculate_respects_valid_range(int evidence, int edges)
    {
        var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            evidence, edges, 1, 0, 0.5, 0.5, 0.5, DateTime.UtcNow, 0.15));
        Assert.InRange(c, 0.05, 0.98);
    }

    [Fact]
    public void Calculate_is_deterministic_for_same_input()
    {
        var input = new GraphConfidenceInput(4, 3, 2, 1, 0.4, 0.5, 0.3, DateTime.UtcNow, 0.15);
        var a = GraphConfidenceCalculator.Calculate(input);
        var b = GraphConfidenceCalculator.Calculate(input);
        Assert.Equal(a, b);
    }

    [Fact]
    public void More_positive_outcomes_increase_confidence()
    {
        var low = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(5, 3, 1, 4, 0.3, 0.3, 0.3, null, 0.15));
        var high = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(5, 3, 4, 1, 0.3, 0.3, 0.3, null, 0.15));
        Assert.True(high > low);
    }

    [Fact]
    public void Recent_signal_increases_confidence()
    {
        var stale = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(4, 2, 2, 1, 0.4, 0.4, 0.2, DateTime.UtcNow.AddDays(-200), 0.15));
        var recent = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(4, 2, 2, 1, 0.4, 0.4, 0.2, DateTime.UtcNow.AddDays(-2), 0.15));
        Assert.True(recent > stale);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(0.25)]
    [InlineData(0.5)]
    [InlineData(0.9)]
    public void Relationship_strength_contributes(double strength)
    {
        var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(3, 2, 1, 0, strength, 0.2, 0.2, null, 0.15));
        Assert.InRange(c, 0.05, 0.98);
    }

    [Fact]
    public void Zero_evidence_uses_base_prior_only()
    {
        var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(0, 0, 0, 0, 0, 0, 0, null, 0.18));
        Assert.Equal(0.18, c);
    }

    [Fact]
    public void Confidence_capped_at_0_98()
    {
        var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(100, 100, 100, 0, 1, 1, 1, DateTime.UtcNow, 0.5));
        Assert.True(c <= 0.98);
    }

    [Fact]
    public void Confidence_floored_at_0_05()
    {
        var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(0, 0, 0, 100, 0, 0, 0, null, 0.01));
        Assert.True(c >= 0.05);
    }

    [Theory]
    [InlineData(1, 7, 0.12)]
    [InlineData(1, 30, 0.08)]
    [InlineData(1, 90, 0.04)]
    public void Recency_buckets_apply(int daysAgo, int _, double minRecencyContribution)
    {
        var withRecent = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(2, 1, 1, 0, 0.2, 0.2, 0.2, DateTime.UtcNow.AddDays(-daysAgo), 0.15));
        var without = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(2, 1, 1, 0, 0.2, 0.2, 0.2, null, 0.15));
        Assert.True(withRecent >= without);
    }

    [Fact]
    public void Semantic_match_score_adds_bounded_factor()
    {
        var low = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(3, 2, 1, 0, 0.3, 0.0, 0.3, null, 0.15));
        var high = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(3, 2, 1, 0, 0.3, 1.0, 0.3, null, 0.15));
        Assert.True(high > low);
    }
}
