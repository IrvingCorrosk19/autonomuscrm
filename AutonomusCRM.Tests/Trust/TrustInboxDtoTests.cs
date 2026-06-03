using AutonomusCRM.Application.Trust;

namespace AutonomusCRM.Tests.Trust;

public class TrustInboxDtoTests
{
    [Fact]
    public void ApprovalInboxItemDto_CarriesRiskAndEntities()
    {
        var dto = new ApprovalInboxItemDto(
            Guid.NewGuid(), Guid.NewGuid(), "Rescue", "ExecutePlaybook", "High churn",
            "pending", DateTime.UtcNow, 88, "Alto", Guid.NewGuid(), "Acme Corp", null, null);

        Assert.Equal("Alto", dto.RiskLevel);
        Assert.Equal(88, dto.DecisionScore);
        Assert.Equal("Acme Corp", dto.CustomerName);
    }
}
