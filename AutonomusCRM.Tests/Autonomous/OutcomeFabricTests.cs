using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Infrastructure.Autonomous;

namespace AutonomusCRM.Tests.Autonomous;

public class OutcomeFabricTests
{
    [Fact]
    public void EnrichDecisionEvidence_SetsExpectedKeys()
    {
        var evidence = new Dictionary<string, object>();
        OutcomeFabricService.EnrichDecisionEvidence(evidence, 1200m, "high");
        Assert.Equal(1200m, evidence["outcomeFabric.expectedImpact"]);
        Assert.Equal("high", evidence["outcomeFabric.expectedRisk"]);
        Assert.Equal("decision_created", evidence["outcomeFabric.learningStatus"]);
    }

    [Fact]
    public void AiDecisionAudit_MarkBusinessOutcome_SetsFlags()
    {
        var audit = AiDecisionAudit.Create(Guid.NewGuid(), "churn", "PreventChurn", 80, "test");
        audit.MarkExecuted();
        audit.MarkBusinessOutcome(true, "renewed");
        Assert.True(audit.BusinessSucceeded);
        Assert.NotNull(audit.BusinessRecordedAt);
    }

    [Fact]
    public void OutcomeFabricStatus_IncompleteWhenNoBusinessOutcome()
    {
        var audit = AiDecisionAudit.Create(Guid.NewGuid(), "expansion", "Upsell", 75, "test");
        audit.MarkExecuted();
        Assert.Null(audit.BusinessRecordedAt);
    }
}
