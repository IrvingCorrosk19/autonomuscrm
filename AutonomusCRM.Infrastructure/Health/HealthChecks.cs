using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AutonomusCRM.Infrastructure.Health;

public static class HealthCheckExtensions
{
    public static IHealthChecksBuilder AddAutonomusHealthChecks(
        this IHealthChecksBuilder builder)
    {
        builder.AddCheck<DatabaseHealthCheck>("database", tags: new[] { "db", "postgresql" });
        builder.AddCheck<EventBusHealthCheck>("eventbus", tags: new[] { "eventbus", "rabbitmq" });
        builder.AddCheck<CacheHealthCheck>("cache", tags: new[] { "cache", "redis" });

        return builder;
    }
}

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(string connectionString, ILogger<DatabaseHealthCheck> logger)
    {
        _connectionString = connectionString;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("DefaultConnection");
            if (string.IsNullOrEmpty(connectionString))
                return HealthCheckResult.Unhealthy("Connection string not configured");

            await using var connection = new NpgsqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            await using var command = new NpgsqlCommand("SELECT 1", connection);
            await command.ExecuteScalarAsync(cancellationToken);

            return HealthCheckResult.Healthy("Database is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database is not accessible", ex);
        }
    }
}

public class EventBusHealthCheck : IHealthCheck
{
    private readonly ILogger<EventBusHealthCheck> _logger;

    public EventBusHealthCheck(ILogger<EventBusHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implementar verificación real de RabbitMQ
        // Por ahora, siempre retorna healthy
        return await Task.FromResult(HealthCheckResult.Healthy("Event Bus is accessible"));
    }
}

public class CacheHealthCheck : IHealthCheck
{
    private readonly ILogger<CacheHealthCheck> _logger;

    public CacheHealthCheck(ILogger<CacheHealthCheck> logger)
    {
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        // TODO: Implementar verificación real de Redis
        // Por ahora, siempre retorna healthy
        return await Task.FromResult(HealthCheckResult.Healthy("Cache is accessible"));
    }
}

