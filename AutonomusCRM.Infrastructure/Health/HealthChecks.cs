using AutonomusCRM.Infrastructure.Caching;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;
using Npgsql;
using RabbitMQ.Client;
using StackExchange.Redis;

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
    private readonly IConfiguration _configuration;
    private readonly ILogger<EventBusHealthCheck> _logger;

    public EventBusHealthCheck(IConfiguration configuration, ILogger<EventBusHealthCheck> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var provider = _configuration["EventBus:Provider"] ?? "RabbitMQ";
        if (provider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            return Task.FromResult(HealthCheckResult.Healthy("InMemory event bus (dev)"));

        try
        {
            var options = _configuration.GetSection("RabbitMQ").Get<RabbitMQOptions>();
            if (options is null || string.IsNullOrEmpty(options.HostName))
                return Task.FromResult(HealthCheckResult.Degraded("RabbitMQ not configured"));

            var factory = new ConnectionFactory
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                VirtualHost = options.VirtualHost ?? "/",
                RequestedConnectionTimeout = TimeSpan.FromSeconds(3)
            };

            using var connection = factory.CreateConnection();
            using var channel = connection.CreateModel();
            return Task.FromResult(HealthCheckResult.Healthy("RabbitMQ is accessible"));
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ health check failed");
            return Task.FromResult(HealthCheckResult.Unhealthy("RabbitMQ is not accessible", ex));
        }
    }
}

public class CacheHealthCheck : IHealthCheck
{
    private readonly IConfiguration _configuration;
    private readonly ICacheService _cacheService;
    private readonly ILogger<CacheHealthCheck> _logger;

    public CacheHealthCheck(
        IConfiguration configuration,
        ICacheService cacheService,
        ILogger<CacheHealthCheck> logger)
    {
        _configuration = configuration;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var redis = _configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redis))
            return HealthCheckResult.Healthy("Memory cache active");

        try
        {
            var mux = await ConnectionMultiplexer.ConnectAsync(redis);
            if (!mux.IsConnected)
                return HealthCheckResult.Unhealthy("Redis not connected");

            var db = mux.GetDatabase();
            await db.PingAsync();
            return HealthCheckResult.Healthy("Redis is accessible");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Redis health check failed — using memory fallback");
            try
            {
                var probeKey = "health:probe";
                await _cacheService.SetAsync(probeKey, new { ok = true }, TimeSpan.FromSeconds(5), cancellationToken);
                var read = await _cacheService.GetAsync<object>(probeKey, cancellationToken);
                return read is not null
                    ? HealthCheckResult.Degraded("Redis down; memory cache OK", ex)
                    : HealthCheckResult.Unhealthy("Cache unavailable", ex);
            }
            catch (Exception inner)
            {
                return HealthCheckResult.Unhealthy("Cache unavailable", inner);
            }
        }
    }
}
