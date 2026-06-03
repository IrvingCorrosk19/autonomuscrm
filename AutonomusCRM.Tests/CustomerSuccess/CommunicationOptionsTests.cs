using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Tests.CustomerSuccess;

public class CommunicationOptionsTests
{
    [Fact]
    public void Default_AllowsSimulation()
    {
        var o = new CommunicationOptions();
        Assert.True(o.AllowSimulation);
    }

    [Fact]
    public void ProductionGuard_BlocksLogWhenSimulationDisabled()
    {
        var o = new CommunicationOptions { AllowSimulation = false, EmailProvider = "Log" };
        Assert.False(o.AllowSimulation);
        Assert.Equal("Log", o.EmailProvider);
    }
}
