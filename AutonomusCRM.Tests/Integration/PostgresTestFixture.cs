using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AutonomusCRM.Tests.Integration;

public sealed class PostgresTestFixture : IAsyncLifetime
{
    private PostgreSqlContainer? _container;

    public string? SkipReason { get; private set; }
    public ApplicationDbContext? Db { get; private set; }

    public async Task InitializeAsync()
    {
        try
        {
            _container = new PostgreSqlBuilder().WithImage("postgres:16-alpine").Build();
            await _container.StartAsync();
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;
            var accessor = new TestTenantAccessor();
            Db = new ApplicationDbContext(options, accessor);
            await Db.Database.MigrateAsync();
        }
        catch (Exception ex)
        {
            SkipReason = $"Docker/Testcontainers unavailable: {ex.Message}";
        }
    }

    public async Task DisposeAsync()
    {
        if (Db != null) await Db.DisposeAsync();
        if (_container != null) await _container.DisposeAsync();
    }
}

internal sealed class TestTenantAccessor : AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor
{
    public Guid? TenantId { get; set; }
    public string? CorrelationId { get; set; }
    public bool BypassTenantFilter { get; set; } = true;
}
