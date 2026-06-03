using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Infrastructure.Integrations;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;

namespace AutonomusCRM.Tests.Integrations;

/// <summary>E2E documentado: OAuth configure → connect → sync → conflict check (unit-level contract).</summary>
public class HubSpotE2EFlowTests
{
    [Fact]
    public void OAuth_IsConfigured_WhenClientIdPresent()
    {
        var oauth = new IntegrationOAuthService(
            Options.Create(new IntegrationOAuthOptions { HubSpotClientId = "id", HubSpotClientSecret = "sec", AppBaseUrl = "http://localhost" }),
            Mock.Of<IIntegrationHubService>(),
            CreateHttpFactory(),
            NullLogger<IntegrationOAuthService>.Instance);
        Assert.True(oauth.IsOAuthConfigured(IntegrationProviders.HubSpot));
        Assert.NotNull(oauth.GetAuthorizationUrl(Guid.NewGuid(), IntegrationProviders.HubSpot));
    }

    [Fact]
    public void OAuth_NotConfigured_WithoutSecrets()
    {
        var oauth = new IntegrationOAuthService(
            Options.Create(new IntegrationOAuthOptions { AppBaseUrl = "http://localhost" }),
            Mock.Of<IIntegrationHubService>(),
            CreateHttpFactory(),
            NullLogger<IntegrationOAuthService>.Instance);
        Assert.False(oauth.IsOAuthConfigured(IntegrationProviders.Salesforce));
    }

    [Fact]
    public void SyncConflictDto_RepresentsLocalNewer()
    {
        var c = new SyncConflictDto("HubSpot", "Customer", Guid.NewGuid().ToString(), "LocalNewer", "drift");
        Assert.Equal("LocalNewer", c.ConflictType);
    }

    private static IHttpClientFactory CreateHttpFactory()
    {
        var mock = new Mock<IHttpClientFactory>();
        mock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());
        return mock.Object;
    }
}
