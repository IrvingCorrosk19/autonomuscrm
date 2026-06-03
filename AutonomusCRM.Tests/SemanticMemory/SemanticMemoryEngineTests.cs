using AutonomusCRM.AI;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Infrastructure.BusinessMemory;
using AutonomusCRM.Infrastructure.SemanticMemory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace AutonomusCRM.Tests.SemanticMemory;

public class SemanticMemoryEngineTests
{
    [Fact]
    public void MemoryEmbedding_RecordUsage_Increments_Count()
    {
        var e = MemoryEmbedding.Create(Guid.NewGuid(), SemanticMemoryConstants.SourceObservation,
            Guid.NewGuid(), "test", new float[] { 1, 2 }, "placeholder", 0.8);
        e.RecordUsage();
        Assert.Equal(1, e.UsageCount);
        Assert.NotNull(e.LastUsedAt);
    }

    [Fact]
    public async Task StoreMemoryAsync_Upserts_By_Source()
    {
        var tenantId = Guid.NewGuid();
        var sourceId = Guid.NewGuid();
        var stored = new List<MemoryEmbedding>();

        var repo = new Mock<ISemanticMemoryRepository>();
        repo.Setup(r => r.GetBySourceAsync(tenantId, SemanticMemoryConstants.SourceLearning, sourceId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => stored.LastOrDefault());
        repo.Setup(r => r.AddEmbeddingAsync(It.IsAny<MemoryEmbedding>(), It.IsAny<CancellationToken>()))
            .Callback<MemoryEmbedding, CancellationToken>((e, _) => stored.Add(e))
            .Returns(Task.CompletedTask);
        repo.Setup(r => r.UpdateEmbeddingAsync(It.IsAny<MemoryEmbedding>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var embed = new Mock<IEmbeddingService>();
        embed.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddingResult(new float[] { 0.5f, 0.5f }, "test", true));

        var business = new Mock<IBusinessMemoryRepository>();
        var service = new SemanticMemoryService(
            repo.Object, business.Object, embed.Object, uow.Object, NullLogger<SemanticMemoryService>.Instance);

        await service.StoreMemoryAsync(tenantId, SemanticMemoryConstants.SourceLearning, sourceId, "first", 0.7);
        await service.StoreMemoryAsync(tenantId, SemanticMemoryConstants.SourceLearning, sourceId, "second", 0.9);

        repo.Verify(r => r.AddEmbeddingAsync(It.IsAny<MemoryEmbedding>(), It.IsAny<CancellationToken>()), Times.Once);
        repo.Verify(r => r.UpdateEmbeddingAsync(It.IsAny<MemoryEmbedding>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SearchAsync_Returns_Ranked_Hits()
    {
        var tenantId = Guid.NewGuid();
        var vector = new float[] { 1, 0, 0, 0, 0, 0, 0, 0 };
        var embeddings = new List<MemoryEmbedding>
        {
            MemoryEmbedding.Create(tenantId, SemanticMemoryConstants.SourceDecision, Guid.NewGuid(),
                "rescue playbook churn", vector, "test"),
            MemoryEmbedding.Create(tenantId, SemanticMemoryConstants.SourceEpisode, Guid.NewGuid(),
                "unrelated marketing", new float[] { 0, 1, 0, 0, 0, 0, 0, 0 }, "test")
        };

        var repo = new Mock<ISemanticMemoryRepository>();
        repo.Setup(r => r.GetByTenantAsync(tenantId, It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        var uow = new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var embed = new Mock<IEmbeddingService>();
        embed.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddingResult(vector, "test", true));

        var service = new SemanticMemoryService(
            repo.Object, new Mock<IBusinessMemoryRepository>().Object, embed.Object, uow.Object,
            NullLogger<SemanticMemoryService>.Instance);

        var hits = await service.SearchAsync(tenantId, "rescue churn playbook");
        Assert.NotEmpty(hits);
        Assert.Contains("rescue", hits[0].Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ConsolidateTenantAsync_Creates_Learning_When_Cluster_Size_Met()
    {
        var tenantId = Guid.NewGuid();
        var observations = Enumerable.Range(0, 12).Select(i =>
            BusinessMemoryObservation.Record(tenantId, "email", "customer asked for discount renewal", "Customer", Guid.NewGuid())
        ).ToList();

        var repo = new Mock<ISemanticMemoryRepository>();
        repo.Setup(r => r.GetObservationsForConsolidationAsync(tenantId, 500, It.IsAny<CancellationToken>()))
            .ReturnsAsync(observations);

        var business = new Mock<IBusinessMemoryRepository>();
        business.Setup(b => b.GetLearningAsync(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((BusinessMemoryLearning?)null);
        business.Setup(b => b.AddLearningAsync(It.IsAny<BusinessMemoryLearning>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var uow = new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>();
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var embed = new Mock<IEmbeddingService>();
        embed.Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmbeddingResult(new float[8], "test", true));

        var service = new SemanticMemoryService(
            repo.Object, business.Object, embed.Object, uow.Object, NullLogger<SemanticMemoryService>.Instance);

        var created = await service.ConsolidateTenantAsync(tenantId);
        Assert.Equal(1, created);
        business.Verify(b => b.AddLearningAsync(It.IsAny<BusinessMemoryLearning>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void CustomerMemoryProfile_Refresh_Updates_Summaries()
    {
        var p = CustomerMemoryProfile.Create(Guid.NewGuid(), Guid.NewGuid());
        p.Refresh("hist", "risk", "pref", "ok", "fail", "email");
        Assert.Equal("hist", p.HistorySummary);
        Assert.Equal("email", p.EffectiveChannelsSummary);
    }

}
