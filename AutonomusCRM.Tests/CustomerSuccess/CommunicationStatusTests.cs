using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Infrastructure.CustomerSuccess;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Tests.CustomerSuccess;

public class CommunicationStatusTests
{
    [Fact]
    public void LogMode_ShowsWarning()
    {
        var svc = new CommunicationStatusService(Options.Create(new CommunicationOptions()));
        var status = svc.GetStatus();
        Assert.False(status.EmailIsLive);
        Assert.Contains("SIMULACIÓN", status.WarningMessage);
    }

    [Fact]
    public void SendGridMode_IsLive()
    {
        var svc = new CommunicationStatusService(Options.Create(new CommunicationOptions
        {
            EmailProvider = "SendGrid",
            SendGridApiKey = "sg-test"
        }));
        var status = svc.GetStatus();
        Assert.True(status.EmailIsLive);
        Assert.Contains("WhatsApp", status.WarningMessage);
    }
}
