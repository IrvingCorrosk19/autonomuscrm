using AutonomusCRM.Application.Trust;
using AutonomusCRM.Infrastructure.Trust;
using Xunit;

namespace AutonomusCRM.Tests.Integration;

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
public class PostgresTrustIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public PostgresTrustIntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task TrustSla_ReturnsEmpty_WhenNoPending()
    {
        if (_fixture.SkipReason != null)
            Assert.Fail($"PostgreSQL integration requiere Docker: {_fixture.SkipReason}");
        if (_fixture.Db == null)
            Assert.Fail("DbContext no inicializado.");

        var svc = new TrustSlaService(_fixture.Db);
        var alerts = await svc.GetOverdueApprovalsAsync(Guid.NewGuid(), 24);
        Assert.Empty(alerts);
    }
}
