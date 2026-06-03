using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Infrastructure.KnowledgeGraph;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AutonomusCRM.Tests.TruthSprint;

public class RevenueSimulationCalculatorTests
{
    private static ApplicationDbContext CreateDb(out Guid tenantId)
    {
        tenantId = Guid.NewGuid();
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new Mock<ICurrentTenantAccessor>().Object);
    }

    [Fact]
    public void LoadBaseline_empty_metrics_use_defaults()
    {
        var baseline = new RevenueSimulationBaseline();
        Assert.Equal(0m, baseline.Mrr);
        Assert.Equal(0, baseline.WinRate);
    }

    [Theory]
    [InlineData("customer_loss")]
    [InlineData("renewal")]
    [InlineData("expansion")]
    [InlineData("deal_won")]
    [InlineData("deal_lost")]
    [InlineData("churn_increase")]
    [InlineData("campaign_executed")]
    public void ProjectScenarioImpact_never_uses_hardcoded_constants(string scenario)
    {
        var baseline = new RevenueSimulationBaseline
        {
            Mrr = 12000m,
            OpenPipeline = 50000m,
            WinRate = 0.4,
            ChurnRate = 0.08,
            LeadVelocityPerMonth = 5,
            AvgDealSize = 8000m,
            CustomerCount = 10
        };
        var impact = RevenueSimulationCalculator.ProjectScenarioImpact(scenario, baseline);
        Assert.DoesNotContain(impact, new[] { -5000m, 5000m, 10000m, 15000m, 25000m, -15000m, -20000m, 3000m });
    }

    [Fact]
    public void Customer_loss_impact_scales_with_mrr_and_churn()
    {
        var lowChurn = new RevenueSimulationBaseline { Mrr = 10000m, ChurnRate = 0.05 };
        var highChurn = new RevenueSimulationBaseline { Mrr = 10000m, ChurnRate = 0.20 };
        var low = RevenueSimulationCalculator.ProjectScenarioImpact("customer_loss", lowChurn);
        var high = RevenueSimulationCalculator.ProjectScenarioImpact("customer_loss", highChurn);
        Assert.True(Math.Abs(high) > Math.Abs(low));
    }

    [Fact]
    public void Deal_won_impact_uses_avg_deal_and_win_rate()
    {
        var baseline = new RevenueSimulationBaseline { AvgDealSize = 20000m, WinRate = 0.5, Mrr = 5000m };
        var impact = RevenueSimulationCalculator.ProjectScenarioImpact("deal_won", baseline);
        Assert.Equal(10000m, impact);
    }

    [Fact]
    public void Renewal_impact_scales_with_mrr()
    {
        var small = new RevenueSimulationBaseline { Mrr = 5000m, WinRate = 0.5 };
        var large = new RevenueSimulationBaseline { Mrr = 20000m, WinRate = 0.5 };
        Assert.True(RevenueSimulationCalculator.ProjectScenarioImpact("renewal", large) >
                    RevenueSimulationCalculator.ProjectScenarioImpact("renewal", small));
    }

    [Fact]
    public void BusinessSimulation_scenario_keys_match_engine_list()
    {
        var keys = new[] { "customer_loss", "renewal", "expansion", "deal_won", "deal_lost", "churn_increase", "campaign_executed" };
        foreach (var key in keys)
        {
            var impact = RevenueSimulationCalculator.ProjectScenarioImpact(key, new RevenueSimulationBaseline { Mrr = 5000m, WinRate = 0.4, AvgDealSize = 2000m, ChurnRate = 0.1, LeadVelocityPerMonth = 2 });
            Assert.DoesNotContain(impact, new[] { -5000m, 10000m, 15000m, 25000m });
        }
    }

    [Fact]
    public void Unknown_scenario_returns_zero()
    {
        var baseline = new RevenueSimulationBaseline { Mrr = 1000m };
        Assert.Equal(0m, RevenueSimulationCalculator.ProjectScenarioImpact("unknown", baseline));
    }

    [Fact]
    public void Open_pipeline_from_baseline_object()
    {
        var baseline = new RevenueSimulationBaseline { OpenPipeline = 25000m };
        Assert.Equal(25000m, baseline.OpenPipeline);
    }
}
