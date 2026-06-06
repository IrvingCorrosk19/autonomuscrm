namespace AutonomusCRM.Tests.Integration;

/// <summary>
/// Skips integration/E2E tests when PostgreSQL/Docker is unavailable instead of failing the run.
/// </summary>
public static class IntegrationTestSkip
{
    public static void IfUnavailable(string? skipReason)
    {
        Skip.If(!string.IsNullOrWhiteSpace(skipReason), skipReason ?? "PostgreSQL unavailable");
    }
}
