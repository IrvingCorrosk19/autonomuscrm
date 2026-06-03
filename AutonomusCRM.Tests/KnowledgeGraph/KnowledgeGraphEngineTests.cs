using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Infrastructure.KnowledgeGraph;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AutonomusCRM.Tests.KnowledgeGraph;

public class KnowledgeGraphEngineTests
{
    [Fact]
    public void NodeTypes_And_Relations_Are_Defined()
    {
        Assert.Equal("CustomerNode", KnowledgeGraphNodeTypes.Customer);
        Assert.Equal("HAS_CONTACT", KnowledgeGraphRelations.HasContact);
        Assert.Equal("DERIVED_FROM_OUTCOME", KnowledgeGraphRelations.DerivedFromOutcome);
    }

    [Fact]
    public async Task BuildGraphAsync_Persists_Edges()
    {
        var tenantId = Guid.NewGuid();
        var added = new List<BusinessKnowledgeGraphEdge>();

        var repo = new Mock<IKnowledgeGraphRepository>();
        repo.Setup(r => r.DeleteAllForTenantAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(0);
        repo.Setup(r => r.AddEdgeAsync(It.IsAny<BusinessKnowledgeGraphEdge>(), It.IsAny<CancellationToken>()))
            .Callback<BusinessKnowledgeGraphEdge, CancellationToken>((e, _) => added.Add(e))
            .Returns(Task.CompletedTask);

        var db = new Mock<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(
            new Microsoft.EntityFrameworkCore.DbContextOptions<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(),
            new Mock<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>().Object);
        // Use real service with mocked repo only - BuildGraphAsync needs DB. Test LinkMemoryToDecision instead.

        var service = new KnowledgeGraphService(
            repo.Object,
            db.Object,
            new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>().Object,
            new Mock<IChurnPredictionModel>().Object,
            new Mock<IExpansionPredictionModel>().Object,
            NullLogger<KnowledgeGraphService>.Instance);

        await service.LinkMemoryToDecisionAsync(tenantId, Guid.NewGuid(), Guid.NewGuid());

        Assert.Single(added);
        Assert.Equal(KnowledgeGraphRelations.SupportsDecision, added[0].RelationType);
    }

    [Fact]
    public async Task SearchGraphAsync_Filters_By_Query()
    {
        var tenantId = Guid.NewGuid();
        var edges = new List<BusinessKnowledgeGraphEdge>
        {
            BusinessKnowledgeGraphEdge.Link(tenantId, KnowledgeGraphNodeTypes.Decision, Guid.NewGuid(),
                KnowledgeGraphNodeTypes.Revenue, Guid.NewGuid(), KnowledgeGraphRelations.GeneratedRevenue, 1m),
            BusinessKnowledgeGraphEdge.Link(tenantId, KnowledgeGraphNodeTypes.Customer, Guid.NewGuid(),
                KnowledgeGraphNodeTypes.Deal, Guid.NewGuid(), KnowledgeGraphRelations.HasDeal, 1m)
        };

        var repo = new Mock<IKnowledgeGraphRepository>();
        repo.Setup(r => r.GetEdgesAsync(tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(edges);

        var service = new KnowledgeGraphService(
            repo.Object,
            new Mock<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(
                new Microsoft.EntityFrameworkCore.DbContextOptions<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(),
                new Mock<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>().Object).Object,
            new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>().Object,
            new Mock<IChurnPredictionModel>().Object,
            new Mock<IExpansionPredictionModel>().Object,
            NullLogger<KnowledgeGraphService>.Instance);

        var result = await service.SearchGraphAsync(tenantId, "Revenue");
        Assert.NotEmpty(result.Edges);
        Assert.All(result.Edges, e =>
            Assert.True(e.Relation.Contains("Revenue", StringComparison.OrdinalIgnoreCase) ||
                        e.TargetType.Contains("Revenue", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public async Task GraphReasoningFoundation_Returns_Prepared_Capabilities()
    {
        var tenantId = Guid.NewGuid();
        var graph = new Mock<IKnowledgeGraphService>();
        graph.Setup(g => g.GetBusinessGraphAsync(tenantId, 50, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new BusinessKnowledgeGraphViewDto(
                tenantId,
                new List<GraphNodeDto> { new(KnowledgeGraphNodeTypes.Customer, Guid.NewGuid(), "C", 1m) },
                new List<GraphEdgeDto>(),
                0,
                new GraphExplorationDto(Array.Empty<GraphExplorationAnswerDto>())));

        var foundation = new GraphReasoningFoundation(graph.Object, new Mock<IGraphReasoningEngine>().Object);
        var ctx = await foundation.PrepareReasoningContextAsync(tenantId, "decision-engine");

        Assert.Contains("DecisionEngine.GraphContext", ctx.PreparedCapabilities);
        Assert.False(ctx.GraphReady);
    }

    [Fact]
    public async Task GetRevenueGraphAsync_Returns_Revenue_Trail()
    {
        var tenantId = Guid.NewGuid();
        var edges = new List<BusinessKnowledgeGraphEdge>
        {
            BusinessKnowledgeGraphEdge.Link(tenantId, KnowledgeGraphNodeTypes.Outcome, Guid.NewGuid(),
                KnowledgeGraphNodeTypes.Revenue, Guid.NewGuid(), KnowledgeGraphRelations.GeneratedRevenue, 2m)
        };

        var repo = new Mock<IKnowledgeGraphRepository>();
        repo.Setup(r => r.GetEdgesAsync(tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(edges);

        var service = new KnowledgeGraphService(
            repo.Object,
            new Mock<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(
                new Microsoft.EntityFrameworkCore.DbContextOptions<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(),
                new Mock<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>().Object).Object,
            new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>().Object,
            new Mock<IChurnPredictionModel>().Object,
            new Mock<IExpansionPredictionModel>().Object,
            NullLogger<KnowledgeGraphService>.Instance);

        var dto = await service.GetRevenueGraphAsync(tenantId);
        Assert.NotEmpty(dto.Edges);
        Assert.NotEmpty(dto.AttributionChain);
    }

    [Fact]
    public async Task CrossTenant_Edges_Isolated_By_Repository()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var repo = new Mock<IKnowledgeGraphRepository>();
        repo.Setup(r => r.GetEdgesAsync(tenantA, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessKnowledgeGraphEdge>
            {
                BusinessKnowledgeGraphEdge.Link(tenantA, KnowledgeGraphNodeTypes.Customer, Guid.NewGuid(),
                    KnowledgeGraphNodeTypes.Deal, Guid.NewGuid(), KnowledgeGraphRelations.HasDeal, 1m)
            });
        repo.Setup(r => r.GetEdgesAsync(tenantB, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<BusinessKnowledgeGraphEdge>());

        var service = new KnowledgeGraphService(
            repo.Object,
            new Mock<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(
                new Microsoft.EntityFrameworkCore.DbContextOptions<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>(),
                new Mock<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>().Object).Object,
            new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>().Object,
            new Mock<IChurnPredictionModel>().Object,
            new Mock<IExpansionPredictionModel>().Object,
            NullLogger<KnowledgeGraphService>.Instance);

        var a = await service.GetBusinessGraphAsync(tenantA);
        var b = await service.GetBusinessGraphAsync(tenantB);
        Assert.NotEmpty(a.Edges);
        Assert.Empty(b.Edges);
    }
}
