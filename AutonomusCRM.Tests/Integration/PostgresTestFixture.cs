using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;

namespace AutonomusCRM.Tests.Integration;

public sealed class PostgresTestFixture : IAsyncLifetime
{
    static PostgresTestFixture()
    {
        NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
    }

    private PostgreSqlContainer? _container;
    private bool _ownsContainer;

    public string? SkipReason { get; private set; }
    public string? ConnectionString { get; private set; }
    public ApplicationDbContext? Db { get; private set; }

    public async Task InitializeAsync()
    {
        var resolved = IntegrationTestEnvironment.ResolvePostgresConnectionString()
                       ?? "Host=localhost;Port=5432;Database=autonomuscrm;Username=postgres;Password=Panama2020$";
        if (resolved != null)
        {
            if (!await CanConnectAsync(resolved))
            {
                SkipReason = IntegrationTestEnvironment.IsCi
                    ? $"PostgreSQL CI service not reachable at {Mask(resolved)}"
                    : $"PostgreSQL not reachable. Start postgres or set INTEGRATION_TEST_CONNECTION_STRING. Tried: {Mask(resolved)}";
                return;
            }

            await InitializeDatabaseAsync(resolved, ownsContainer: false);
            return;
        }

        try
        {
            _container = new PostgreSqlBuilder()
                .WithImage("postgres:16-alpine")
                .WithDatabase("autonomuscrm_test")
                .WithUsername("postgres")
                .WithPassword("test_password")
                .Build();
            await _container.StartAsync();
            await InitializeDatabaseAsync(_container.GetConnectionString(), ownsContainer: true);
        }
        catch (Exception ex)
        {
            SkipReason =
                $"PostgreSQL unavailable: {ex.Message}. " +
                "Set INTEGRATION_TEST_CONNECTION_STRING or ConnectionStrings__DefaultConnection (GitHub Actions postgres service). " +
                "Local fallback requires Docker Desktop for Testcontainers.";
        }
    }

    private async Task InitializeDatabaseAsync(string connectionString, bool ownsContainer)
    {
        _ownsContainer = ownsContainer;
        ConnectionString = connectionString;
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;
        var accessor = new TestTenantAccessor();
        Db = new ApplicationDbContext(options, accessor);
        await Db.Database.MigrateAsync();
    }

    private static async Task<bool> CanConnectAsync(string connectionString)
    {
        for (var attempt = 1; attempt <= 15; attempt++)
        {
            try
            {
                await using var conn = new NpgsqlConnection(connectionString);
                await conn.OpenAsync();
                return true;
            }
            catch (Exception) when (attempt < 15)
            {
                await Task.Delay(TimeSpan.FromSeconds(2));
            }
        }

        return false;
    }

    private static string Mask(string cs) =>
        cs.Contains("Password=", StringComparison.OrdinalIgnoreCase)
            ? cs[..Math.Min(cs.IndexOf("Password=", StringComparison.OrdinalIgnoreCase), cs.Length)] + "Password=***"
            : cs;

    public async Task DisposeAsync()
    {
        if (Db != null) await Db.DisposeAsync();
        if (_ownsContainer && _container != null) await _container.DisposeAsync();
    }
}

public sealed class TestTenantAccessor : AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor
{
    public Guid? TenantId { get; set; }
    public string? CorrelationId { get; set; }
    public bool BypassTenantFilter { get; set; } = true;
}
