using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Infrastructure.KnowledgeGraph;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace AutonomusCRM.Tests.KnowledgeGraph;

public class PhaseDGraphReasoningTests
{
    private static ApplicationDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        return new ApplicationDbContext(options, new Mock<ICurrentTenantAccessor>().Object);
    }

    [Fact]
    public void BusinessSimulationEngine_Lists_Real_Scenarios()
    {
        using var db = CreateDb();
        var engine = new BusinessSimulationEngine(
            new Mock<IKnowledgeGraphService>().Object,
            new Mock<IGraphReasoningEngine>().Object,
            db);
        var scenarios = engine.GetAvailableScenarios();
        Assert.Contains("customer_loss", scenarios);
        Assert.Contains("deal_won", scenarios);
    }

    [Fact]
    public void GraphReasoning_confidence_is_evidence_based_not_fixed_literal()
    {
        var withEvidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            6, 4, 3, 1, 0.6, 0.7, 0.5, DateTime.UtcNow, 0.2));
        var withoutEvidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            0, 0, 0, 0, 0, 0, 0, null, 0.12));
        Assert.True(withEvidence > withoutEvidence);
        Assert.NotEqual(0.82, withEvidence);
        Assert.NotEqual(0.55, withoutEvidence);
    }
}
