using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Infrastructure.Autonomous;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AutonomusCRM.Tests.Autonomous;

public class AutonomousPlatformGateTests
{
    [Fact]
    public async Task IsAutonomousExecutionAllowed_WhenGloballyDisabled_ReturnsFalse()
    {
        var tenants = new Mock<AutonomusCRM.Application.Common.Interfaces.ITenantRepository>();
        var gate = new AutonomousPlatformGate(
            Options.Create(new AutonomousPlatformOptions { Enabled = false }),
            tenants.Object,
            NullLogger<AutonomousPlatformGate>.Instance);

        var allowed = await gate.IsAutonomousExecutionAllowedAsync(Guid.NewGuid());
        Assert.False(allowed);
        tenants.Verify(t => t.IsKillSwitchEnabledAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task IsAutonomousExecutionAllowed_WhenKillSwitch_ReturnsFalse()
    {
        var tenantId = Guid.NewGuid();
        var tenants = new Mock<AutonomusCRM.Application.Common.Interfaces.ITenantRepository>();
        tenants.Setup(t => t.IsKillSwitchEnabledAsync(tenantId, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        var gate = new AutonomousPlatformGate(
            Options.Create(new AutonomousPlatformOptions { Enabled = true }),
            tenants.Object,
            NullLogger<AutonomousPlatformGate>.Instance);

        Assert.False(await gate.IsAutonomousExecutionAllowedAsync(tenantId));
    }
}
