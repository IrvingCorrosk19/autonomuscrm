using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Infrastructure.Billing;
using AutonomusCRM.Infrastructure.Integrations;
using Microsoft.Extensions.Options;
using Moq;

namespace AutonomusCRM.Tests.PreConnection;

public class PreConnectionCertificationTests
{
    [Fact]
    public void IntegrationProviderCatalog_Lists_All_Providers()
    {
        Assert.Equal(11, IntegrationProviderCatalog.All.Length);
        Assert.Contains(IntegrationProviders.OpenAI, IntegrationProviderCatalog.All);
        Assert.Contains(IntegrationProviders.HubSpot, IntegrationProviderCatalog.All);
    }

    [Fact]
    public void SecretMaskingService_Masks_Tokens()
    {
        var svc = new SecretMaskingService();
        var masked = svc.Mask("sk-1234567890abcdef");
        Assert.Contains("…", masked);
        Assert.DoesNotContain("abcdef", masked);
    }

    [Fact]
    public async Task SmokeTest_Returns_Blocked_Without_Credentials()
    {
        var health = new Mock<IIntegrationHealthService>();
        health.Setup(h => h.GetDashboardAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new IntegrationHealthDashboardDto(
                Guid.NewGuid(),
                new List<IntegrationHealthItemDto>
                {
                    new(IntegrationProviders.SendGrid, IntegrationHealthStates.Disconnected, false, false, false,
                        "global", null, null, "Missing", ["Communications:SendGridApiKey"])
                },
                false, "plain"));

        var smoke = new IntegrationSmokeTestService(health.Object);
        var result = await smoke.RunAsync(IntegrationProviders.SendGrid, Guid.NewGuid());
        Assert.Equal(IntegrationHealthStates.Blocked, result.Status);
        Assert.True(result.RequiresCredentials);
    }

    [Fact]
    public async Task IntegrationHealthService_Reports_Encryption_Badge()
    {
        var repo = new Mock<ITenantIntegrationRepository>();
        repo.Setup(r => r.ListAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<TenantIntegrationConnection>());

        var oauth = new Mock<IIntegrationOAuthService>();
        oauth.Setup(o => o.IsOAuthConfigured(It.IsAny<string>())).Returns(false);

        var embeddings = new Mock<IProductionEmbeddingProvider>();
        embeddings.Setup(e => e.GetStatus()).Returns(new ProductionEmbeddingStatus(
            "local-deterministic", false, "fallback", false, false));

        var protector = new Mock<IIntegrationTokenProtector>();
        protector.Setup(p => p.EncryptionConfigured).Returns(true);

        var svc = new IntegrationHealthService(
            repo.Object,
            oauth.Object,
            embeddings.Object,
            protector.Object,
            Options.Create(new CommunicationOptions()),
            Options.Create(new StripeBillingOptions()),
            Options.Create(new TwilioOptions()));

        var dash = await svc.GetDashboardAsync(Guid.NewGuid());
        Assert.True(dash.SecretEncryptionConfigured);
        Assert.Equal(11, dash.Providers.Count);
    }
}
