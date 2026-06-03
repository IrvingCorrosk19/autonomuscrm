namespace AutonomusCRM.Tests.Integration;

/// <summary>Resolves PostgreSQL for integration tests: env vars first, then CI defaults, then Testcontainers.</summary>
public static class IntegrationTestEnvironment
{
    public const string DefaultCiConnectionString =
        "Host=localhost;Port=5432;Database=autonomuscrm_test;Username=postgres;Password=test_password";

    public static string? ResolvePostgresConnectionString()
    {
        var candidates = new[]
        {
            Environment.GetEnvironmentVariable("INTEGRATION_TEST_CONNECTION_STRING"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"),
            Environment.GetEnvironmentVariable("TEST_DATABASE_URL")
        };

        foreach (var cs in candidates)
        {
            if (!string.IsNullOrWhiteSpace(cs))
                return cs;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase))
            return DefaultCiConnectionString;

        return null;
    }

    public static bool IsCi => string.Equals(Environment.GetEnvironmentVariable("GITHUB_ACTIONS"), "true", StringComparison.OrdinalIgnoreCase);
}
