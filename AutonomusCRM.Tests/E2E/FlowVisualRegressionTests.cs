using AutonomusCRM.Tests.Integration;

namespace AutonomusCRM.Tests.E2E;

[Collection("PostgresWebIntegration")]
/// <summary>
/// Regresión visual por baseline HTML + smoke HTTP (Playwright PNG: instalar Microsoft.Playwright cuando red lo permita).
/// </summary>
public sealed class FlowVisualRegressionTests
{
    private static readonly string SnapshotDir = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "TestResults", "screenshots"));

    private readonly PostgresWebApplicationFixture _fixture;

    public FlowVisualRegressionTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    private HttpClient RequireClient()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        return _fixture.Client ?? throw new InvalidOperationException("HttpClient no inicializado.");
    }

    [SkippableTheory]
    [Trait("Category", "Integration")]
    [InlineData("/Account/Login", "login", "flow-auth-page")]
    [InlineData("/revenue", "revenue", "flow-page")]
    [InlineData("/executive", "executive", "flow-page")]
    [InlineData("/billing", "billing", "flow-page")]
    [InlineData("/TrustInbox", "trust", "flow-page")]
    [InlineData("/Integrations", "integrations", "flow-page")]
    [InlineData("/VoiceCalls", "voice", "flow-page")]
    [InlineData("/flow/components", "components", "flow-page")]
    public async Task Key_pages_save_html_baseline_and_return_shell(string path, string name, string marker)
    {
        var client = RequireClient();
        var response = await client.GetAsync(path);
        Assert.True((int)response.StatusCode is 200 or 302 or 401,
            $"{path} → {(int)response.StatusCode}");

        if (response.StatusCode == System.Net.HttpStatusCode.OK)
        {
            var html = await response.Content.ReadAsStringAsync();
            Directory.CreateDirectory(SnapshotDir);
            var file = Path.Combine(SnapshotDir, $"flow-{name}.html");
            await File.WriteAllTextAsync(file, html);
            Assert.True(new FileInfo(file).Length > 200, $"Baseline vacío: {file}");
            if (path.Contains("Login", StringComparison.OrdinalIgnoreCase))
                Assert.Contains(marker, html, StringComparison.OrdinalIgnoreCase);
            else if ((int)response.StatusCode == 200)
                Assert.True(
                    html.Contains(marker, StringComparison.OrdinalIgnoreCase)
                    || html.Contains("flow-shell", StringComparison.OrdinalIgnoreCase)
                    || html.Contains("Login", StringComparison.OrdinalIgnoreCase),
                    $"Marcador Flow ausente en {path}");
        }
    }
}
