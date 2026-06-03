using System.Net;
using AutonomusCRM.Tests.Integration;

namespace AutonomusCRM.Tests.E2E;

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
public class FlowPhase3UiE2ETests : IClassFixture<PostgresWebApplicationFixture>
{
    private readonly PostgresWebApplicationFixture _fixture;

    public FlowPhase3UiE2ETests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [Theory]
    [Trait("Category", "Integration")]
    [InlineData("/revenue")]
    [InlineData("/executive")]
    [InlineData("/billing")]
    [InlineData("/Customer360")]
    [InlineData("/TrustInbox")]
    [InlineData("/")]
    public async Task Phase3_pages_respond_without_server_error(string path)
    {
        if (_fixture.SkipReason != null)
            Assert.Fail($"PostgreSQL integration: {_fixture.SkipReason}");
        var client = _fixture.Client ?? throw new InvalidOperationException("HttpClient no inicializado.");
        var response = await client.GetAsync(path);
        Assert.True(
            response.StatusCode is HttpStatusCode.OK
                or HttpStatusCode.Redirect
                or HttpStatusCode.Found
                or HttpStatusCode.Unauthorized,
            $"Expected OK/redirect/auth for {path}, got {response.StatusCode}");
        Assert.NotEqual(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
