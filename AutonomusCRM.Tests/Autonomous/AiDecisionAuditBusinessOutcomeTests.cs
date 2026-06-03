using AutonomusCRM.Application.Autonomous;

namespace AutonomusCRM.Tests.Autonomous;

public class AiDecisionAuditBusinessOutcomeTests
{
    [Fact]
    public void MarkBusinessOutcome_SetsFields()
    {
        var audit = AiDecisionAudit.Create(Guid.NewGuid(), "Renewal", "CloseDeal", 80, "test");
        audit.MarkBusinessOutcome(true, "Deal won");
        Assert.True(audit.BusinessSucceeded);
        Assert.Equal("Deal won", audit.BusinessOutcomeDetail);
        Assert.NotNull(audit.BusinessRecordedAt);
    }
}
