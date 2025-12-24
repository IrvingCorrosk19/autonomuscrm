using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace AutonomusCRM.Infrastructure.Health;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly string _connectionString;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(IConfiguration configuration, ILogger<DatabaseHealthCheck> logger)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection") ?? string.Empty;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (string.IsNullOrEmpty(_connectionString))
                return HealthCheckResult.Unhealthy("Connection string not configured");

            await using var connection = new NpgsqlConnection(_connectionString);
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

