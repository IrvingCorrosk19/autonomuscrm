using AutonomusCRM.Application.DataPlatform;

namespace AutonomusCRM.Tests.DataPlatform;

public class CdpStreamEventTests
{
    [Fact]
    public void Create_SetsOccurredAt()
    {
        var evt = CdpStreamEvent.Create(Guid.NewGuid(), "customer.updated", Guid.NewGuid(),
            new Dictionary<string, object?> { ["field"] = "email" });
        Assert.Equal("customer.updated", evt.EventType);
        Assert.True(evt.OccurredAt <= DateTime.UtcNow);
    }
}
