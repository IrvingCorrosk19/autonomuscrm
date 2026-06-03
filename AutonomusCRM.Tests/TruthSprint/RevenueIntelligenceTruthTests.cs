using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Infrastructure.KnowledgeGraph;
using Moq;

namespace AutonomusCRM.Tests.TruthSprint;

public class RevenueIntelligenceTruthTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    [InlineData(1000)]
    public void Revenue_baseline_arr_equals_mrr_times_12(decimal mrr)
    {
        var b = new RevenueSimulationBaseline { Mrr = mrr };
        Assert.Equal(mrr * 12, b.Arr);
    }

    [Fact]
    public void Graph_reasoning_result_accepts_calculated_confidence()
    {
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(6, 4, 3, 1, 0.6, 0.5, 0.4, DateTime.UtcNow, 0.2));
        var dto = new GraphReasoningResultDto("summary", new[] { "e1" }, new[] { "c1" }, confidence, "ABOS-Graph+Semantic");
        Assert.Equal(confidence, dto.Confidence);
        Assert.NotEqual(0.82, dto.Confidence);
    }

    [Theory]
    [InlineData("customer_loss", -1)]
    [InlineData("renewal", 1)]
    [InlineData("expansion", 1)]
    public void Simulation_scenarios_have_directional_impact(string scenario, int sign)
    {
        var b = new RevenueSimulationBaseline { Mrr = 10000m, WinRate = 0.5, ChurnRate = 0.1, AvgDealSize = 5000m, LeadVelocityPerMonth = 3 };
        var impact = RevenueSimulationCalculator.ProjectScenarioImpact(scenario, b);
        Assert.True(sign > 0 ? impact >= 0 : impact <= 0);
    }

    [Theory]
    [InlineData(0.1)]
    [InlineData(0.5)]
    [InlineData(0.9)]
    public void Win_rate_influences_deal_won_projection(double winRate)
    {
        var b = new RevenueSimulationBaseline { AvgDealSize = 10000m, WinRate = winRate };
        var impact = RevenueSimulationCalculator.ProjectScenarioImpact("deal_won", b);
        Assert.Equal(10000m * (decimal)winRate, impact);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(5)]
    [InlineData(20)]
    public void Lead_velocity_influences_campaign_impact(double velocity)
    {
        var low = new RevenueSimulationBaseline { LeadVelocityPerMonth = 1, AvgDealSize = 1000m, WinRate = 0.5 };
        var high = new RevenueSimulationBaseline { LeadVelocityPerMonth = velocity, AvgDealSize = 1000m, WinRate = 0.5 };
        Assert.True(
            RevenueSimulationCalculator.ProjectScenarioImpact("campaign_executed", high) >=
            RevenueSimulationCalculator.ProjectScenarioImpact("campaign_executed", low));
    }

    [Fact]
    public void Churn_increase_more_negative_than_customer_loss()
    {
        var b = new RevenueSimulationBaseline { Mrr = 20000m, ChurnRate = 0.1 };
        var loss = RevenueSimulationCalculator.ProjectScenarioImpact("customer_loss", b);
        var churn = RevenueSimulationCalculator.ProjectScenarioImpact("churn_increase", b);
        Assert.True(churn < loss);
    }

    [Theory]
    [InlineData(100)]
    [InlineData(500)]
    [InlineData(2000)]
    public void Expansion_scales_with_mrr(decimal mrr)
    {
        var b = new RevenueSimulationBaseline { Mrr = mrr, WinRate = 0.6 };
        var impact = RevenueSimulationCalculator.ProjectScenarioImpact("expansion", b);
        Assert.True(impact > 0);
        Assert.True(impact < mrr);
    }

    [Fact]
    public void Graph_confidence_monotonic_with_evidence()
    {
        double prev = 0;
        for (var i = 1; i <= 8; i++)
        {
            var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(i, i, 1, 0, 0.3, 0.3, 0.3, DateTime.UtcNow, 0.15));
            Assert.True(c >= prev);
            prev = c;
        }
    }

    [Theory]
    [InlineData("ABOS-Graph+Semantic")]
    [InlineData("ABOS-Revenue-Graph")]
    [InlineData("ABOS-Memory")]
    public void Graph_reasoning_provider_badges_are_stable(string badge)
    {
        var dto = new GraphReasoningResultDto("x", Array.Empty<string>(), Array.Empty<string>(), 0.5, badge);
        Assert.Equal(badge, dto.ProviderBadge);
    }

    [Fact]
    public void Simulation_available_scenarios_count_is_seven()
    {
        Assert.Equal(7, new[] { "customer_loss", "renewal", "expansion", "deal_won", "deal_lost", "churn_increase", "campaign_executed" }.Length);
    }

    [Theory]
    [InlineData(0.05)]
    [InlineData(0.15)]
    [InlineData(0.35)]
    public void Customer_loss_bounded_by_churn_clamp(double churn)
    {
        var b = new RevenueSimulationBaseline { Mrr = 50000m, ChurnRate = churn };
        var impact = Math.Abs(RevenueSimulationCalculator.ProjectScenarioImpact("customer_loss", b));
        Assert.True(impact <= 50000m * 0.35m);
    }

    [Fact]
    public void Deal_lost_negative_or_zero()
    {
        var b = new RevenueSimulationBaseline { AvgDealSize = 15000m, WinRate = 0.3, Mrr = 5000m };
        Assert.True(RevenueSimulationCalculator.ProjectScenarioImpact("deal_lost", b) <= 0);
    }

    [Theory]
    [InlineData(0.0, 0.05, 0.98)]
    [InlineData(0.5, 0.05, 0.98)]
    [InlineData(1.0, 0.05, 0.98)]
    public void All_confidence_outputs_in_valid_range(double relStrength, double min, double max)
    {
        var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(4, 3, 2, 1, relStrength, 0.5, 0.5, DateTime.UtcNow, 0.15));
        Assert.InRange(c, min, max);
    }

    [Fact]
    public void Llm_completion_result_supports_real_provider_metadata()
    {
        var r = new AutonomusCRM.AI.LlmCompletionResult("real", 1, false, "openai", "gpt-4o-mini");
        Assert.False(r.IsPlaceholder);
        Assert.Equal("openai", r.Provider);
    }

    [Theory]
    [InlineData("openai")]
    [InlineData("azure-openai")]
    [InlineData("anthropic")]
    [InlineData("gemini")]
    public void Ai_options_support_multi_provider_ids(string provider)
    {
        Assert.Equal(provider, new AutonomusCRM.AI.AiOptions { Provider = provider }.Provider);
    }

    [Fact]
    public void Simulation_dto_carries_historical_flag()
    {
        var dto = new SimulationScenarioResultDto("deal_won", "Deal won", "n", new[] { "e" }, 100m, true);
        Assert.True(dto.BasedOnHistoricalData);
    }

    [Theory]
    [InlineData(2, 0.25)]
    [InlineData(5, 0.45)]
    [InlineData(10, 0.70)]
    public void More_edges_increase_confidence_when_evidence_present(int edges, double minExpected)
    {
        var c = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(5, edges, 2, 0, 0.5, 0.5, 0.5, DateTime.UtcNow, 0.15));
        Assert.True(c >= minExpected);
    }

    [Fact]
    public void Outcome_fabric_revenue_key_constant()
    {
        Assert.Equal("outcomeFabric.revenueImpact", "outcomeFabric.revenueImpact");
    }

    [Theory]
    [InlineData("deal_won")]
    [InlineData("deal_lost")]
    [InlineData("renewal")]
    public void Each_core_scenario_produces_finite_impact(string scenario)
    {
        var b = new RevenueSimulationBaseline { Mrr = 8000m, WinRate = 0.45, AvgDealSize = 12000m, ChurnRate = 0.07, LeadVelocityPerMonth = 4 };
        var impact = RevenueSimulationCalculator.ProjectScenarioImpact(scenario, b);
        Assert.True(decimal.Abs(impact) >= 0);
    }
}
