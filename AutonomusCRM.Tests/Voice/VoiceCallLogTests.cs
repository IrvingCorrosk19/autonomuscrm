using AutonomusCRM.Application.Voice;

namespace AutonomusCRM.Tests.Voice;

public class VoiceCallLogTests
{
    [Fact]
    public void Create_AssociatesEntities()
    {
        var tenantId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var log = VoiceCallLog.Create(tenantId, "+50760000000", "outbound", 120, "connected", customerId);
        Assert.Equal(customerId, log.CustomerId);
        Assert.Equal("pending", log.TranscriptStatus);
    }
}
