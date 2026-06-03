using AutonomusCRM.Infrastructure.Billing;
using Microsoft.Extensions.Configuration;
using Moq;

namespace AutonomusCRM.Tests.Billing;

public class StripeWebhookSecurityTests
{
    [Fact]
    public async Task HandleWebhook_Production_WithoutSecret_Throws()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ASPNETCORE_ENVIRONMENT"] = "Production",
                ["Stripe:WebhookSecret"] = ""
            })
            .Build();

        var svc = new StripeBillingService(
            config,
            Mock.Of<AutonomusCRM.Application.Billing.ITenantBillingRepository>(),
            Mock.Of<AutonomusCRM.Application.Common.Interfaces.ITenantRepository>(),
            Mock.Of<AutonomusCRM.Application.Common.Interfaces.ICustomerRepository>(),
            Mock.Of<AutonomusCRM.Application.Autonomous.IOutcomeAttributionService>(),
            Mock.Of<Microsoft.Extensions.Logging.ILogger<StripeBillingService>>());

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            svc.HandleWebhookAsync("{}", "", CancellationToken.None));
    }
}
