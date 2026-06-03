using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Infrastructure.BusinessMemory;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;



namespace AutonomusCRM.Tests.BusinessMemory;



public class BusinessMemoryEngineTests

{

    [Fact]

    public async Task Pipeline_Captures_DealClosed_Persists_Memory_Event_Outcome_Learning()

    {

        var tenantId = Guid.NewGuid();

        var dealId = Guid.NewGuid();

        var saved = new List<BusinessMemoryRoot>();



        var repo = new Mock<IBusinessMemoryRepository>();

        repo.Setup(r => r.GetByEpisodeKeyAsync(tenantId, It.IsAny<string>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync((BusinessMemoryRoot?)null);

        repo.Setup(r => r.AddMemoryAsync(It.IsAny<BusinessMemoryRoot>(), It.IsAny<CancellationToken>()))

            .Callback<BusinessMemoryRoot, CancellationToken>((m, _) => saved.Add(m))

            .Returns(Task.CompletedTask);



        var uow = new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>();

        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);



        var semantic = new Mock<ISemanticMemoryService>();
        semantic.Setup(s => s.StoreMemoryAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(MemoryEmbedding.Create(tenantId, SemanticMemoryConstants.SourceEpisode, Guid.NewGuid(), "t", new float[8], "test"));

        var graphRepo = new Mock<IKnowledgeGraphRepository>();
        graphRepo.Setup(g => g.AddEdgeAsync(It.IsAny<AutonomusCRM.Application.EnterpriseAI.BusinessKnowledgeGraphEdge>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var pipeline = new BusinessMemoryPipeline(
            repo.Object, new Mock<IAiDecisionAuditRepository>().Object, uow.Object, semantic.Object, graphRepo.Object,
            NullLogger<BusinessMemoryPipeline>.Instance);



        await pipeline.CaptureFromDomainEventAsync(

            new DealClosedEvent(dealId, tenantId, DateTime.UtcNow, 50000m));



        Assert.Single(saved);

        Assert.Equal(dealId, saved[0].SubjectId);

        repo.Verify(r => r.AddOutcomeAsync(It.IsAny<BusinessMemoryOutcome>(), It.IsAny<CancellationToken>()), Times.Once);

        repo.Verify(r => r.GetLearningAsync(tenantId, "deal.won", It.IsAny<CancellationToken>()), Times.Once);

        repo.Verify(r => r.AddLearningAsync(It.IsAny<BusinessMemoryLearning>(), It.IsAny<CancellationToken>()), Times.Once);

    }



    [Fact]

    public async Task Pipeline_Skips_When_EpisodeKey_Exists()

    {

        var tenantId = Guid.NewGuid();

        var repo = new Mock<IBusinessMemoryRepository>();

        repo.Setup(r => r.GetByEpisodeKeyAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))

            .ReturnsAsync(BusinessMemoryRoot.CreateEpisode(tenantId, "Deal", Guid.NewGuid(), "x", "t", "s"));



        var semantic = new Mock<ISemanticMemoryService>();
        var graphRepo = new Mock<IKnowledgeGraphRepository>();
        var pipeline = new BusinessMemoryPipeline(
            repo.Object, new Mock<IAiDecisionAuditRepository>().Object,
            new Mock<AutonomusCRM.Application.Common.Interfaces.IUnitOfWork>().Object,
            semantic.Object, graphRepo.Object,
            NullLogger<BusinessMemoryPipeline>.Instance);



        await pipeline.CaptureFromDomainEventAsync(

            new DealClosedEvent(Guid.NewGuid(), tenantId, DateTime.UtcNow, 1m));



        repo.Verify(r => r.AddMemoryAsync(It.IsAny<BusinessMemoryRoot>(), It.IsAny<CancellationToken>()), Times.Never);

    }



    [Fact]

    public void Learning_ApplyOutcome_Updates_SuccessRate()

    {

        var learning = BusinessMemoryLearning.Start(Guid.NewGuid(), "retention.discount", "apply_discount");

        learning.ApplyOutcome(true, "renewed");

        learning.ApplyOutcome(false, "churned");

        Assert.Equal(50m, learning.SuccessRate);

    }



    [Fact]

    public void MemoryRoot_CreateEpisode_Sets_Importance_Clamped()

    {

        var m = BusinessMemoryRoot.CreateEpisode(Guid.NewGuid(), "Customer", Guid.NewGuid(),

            "k", "Title", "Summary", importance: 99);

        Assert.Equal(10, m.Importance);

    }

}


