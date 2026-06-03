using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Infrastructure.KnowledgeGraph;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AutonomusCRM.Tests.Integration;

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
public class KnowledgeGraphIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public KnowledgeGraphIntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task BuildGraph_Persists_Edges_In_Postgres()
    {
        if (_fixture.SkipReason != null)
            Assert.Fail($"PostgreSQL integration requiere Docker: {_fixture.SkipReason}");
        if (_fixture.Db == null)
            Assert.Fail("DbContext no inicializado.");

        var tenantId = Guid.NewGuid();
        var repo = new KnowledgeGraphRepository(_fixture.Db);
        var uow = new Mock<IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1)
            .Callback(() => _fixture.Db.SaveChanges());

        var churn = new Mock<AutonomusCRM.Application.EnterpriseAI.IChurnPredictionModel>();
        churn.Setup(c => c.PredictAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AutonomusCRM.Application.EnterpriseAI.ChurnMlPredictionDto>());
        var expansion = new Mock<AutonomusCRM.Application.EnterpriseAI.IExpansionPredictionModel>();
        expansion.Setup(e => e.PredictAsync(tenantId, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<AutonomusCRM.Application.EnterpriseAI.ExpansionMlPredictionDto>());

        var service = new KnowledgeGraphService(
            repo, _fixture.Db, uow.Object, churn.Object, expansion.Object,
            NullLogger<KnowledgeGraphService>.Instance);

        var count = await service.BuildGraphAsync(tenantId);
        var stored = await repo.CountEdgesAsync(tenantId);
        Assert.True(stored >= count);
    }
}
