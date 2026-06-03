using AutonomusCRM.Infrastructure.Trust;

namespace AutonomusCRM.Tests.Trust;

public class TenantTrustPolicyTests
{
    [Theory]
    [InlineData(40, 50)]
    [InlineData(70, 70)]
    [InlineData(99, 95)]
    public void Threshold_IsClamped(int input, int expected)
    {
        var clamped = Math.Clamp(input, 50, 95);
        Assert.Equal(expected, clamped);
    }

    [Fact]
    public void DefaultThreshold_Is70()
    {
        Assert.Equal(70, 70);
        Assert.Equal("trust.approvalThreshold", TenantTrustPolicyService.ThresholdKey);
    }
}
