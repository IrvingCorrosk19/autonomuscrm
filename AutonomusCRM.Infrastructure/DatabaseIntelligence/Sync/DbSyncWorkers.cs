using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncProcessingOptions
{
    public DbSyncProcessingMode ProcessingMode { get; set; } = DbSyncProcessingMode.InProcess;
    public string SyncQueueName { get; set; } = "db-intelligence.sync.jobs";
    public string SyncDeadLetterQueueName { get; set; } = "db-intelligence.sync.jobs.dlq";
}

public enum DbSyncProcessingMode
{
    InProcess,
    RabbitMQ
}

public record DbSyncJobMessage(Guid JobId, Guid TenantId);

public interface IDbSyncJobQueue
{
    void Enqueue(Guid jobId);
    bool TryDequeue(out Guid jobId);
}

public sealed class DbSyncInProcessJobQueue : IDbSyncJobQueue
{
    private readonly Queue<Guid> _queue = new();

    public void Enqueue(Guid jobId) => _queue.Enqueue(jobId);

    public bool TryDequeue(out Guid jobId)
    {
        if (_queue.Count == 0)
        {
            jobId = Guid.Empty;
            return false;
        }
        jobId = _queue.Dequeue();
        return true;
    }
}

public sealed class DbSyncDispatcher : IDbSyncDispatcher
{
    private readonly IDbSyncJobQueue _queue;
    private readonly DbSyncProcessingOptions _options;
    private readonly RabbitMQOptions _rabbitOptions;
    private readonly ILogger<DbSyncDispatcher> _logger;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;

    public DbSyncDispatcher(
        IDbSyncJobQueue queue,
        IOptions<DbSyncProcessingOptions> options,
        IOptions<RabbitMQOptions> rabbitOptions,
        ILogger<DbSyncDispatcher> logger)
    {
        _queue = queue;
        _options = options.Value;
        _rabbitOptions = rabbitOptions.Value;
        _logger = logger;
    }

    public Task EnqueueSyncJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        if (_options.ProcessingMode == DbSyncProcessingMode.RabbitMQ)
        {
            if (!TryPublishRabbit(tenantId, jobId))
                throw new InvalidOperationException($"RabbitMQ dispatch failed for sync job {jobId}.");
            return Task.CompletedTask;
        }

        _queue.Enqueue(jobId);
        return Task.CompletedTask;
    }

    private bool TryPublishRabbit(Guid tenantId, Guid jobId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_rabbitOptions.HostName)) return false;
            EnsureRabbitChannel();
            var payload = JsonSerializer.SerializeToUtf8Bytes(new DbSyncJobMessage(jobId, tenantId));
            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            _channel.BasicPublish("", _options.SyncQueueName, props, payload);
            _logger.LogInformation("DIP sync job {JobId} dispatched to {Queue}", jobId, _options.SyncQueueName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ dispatch failed for sync job {JobId}", jobId);
            return false;
        }
    }

    internal void EnsureRabbitChannel()
    {
        lock (_sync)
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true) return;
            var factory = new ConnectionFactory
            {
                HostName = _rabbitOptions.HostName,
                Port = _rabbitOptions.Port,
                UserName = _rabbitOptions.UserName,
                Password = _rabbitOptions.Password,
                VirtualHost = _rabbitOptions.VirtualHost ?? "/",
                AutomaticRecoveryEnabled = true,
                DispatchConsumersAsync = true
            };
            _connection?.Dispose();
            _channel?.Dispose();
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            _channel.QueueDeclare(_options.SyncQueueName, true, false, false);
            _channel.QueueDeclare(_options.SyncDeadLetterQueueName, true, false, false);
            _channel.BasicQos(0, 1, false);
        }
    }

    internal IModel? GetChannel() => _channel;
}

public sealed class DbSyncBackgroundWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IDbSyncJobQueue _queue;
    private readonly DbSyncProcessingOptions _options;
    private readonly ILogger<DbSyncBackgroundWorker> _logger;

    public DbSyncBackgroundWorker(
        IServiceScopeFactory scopeFactory,
        IDbSyncJobQueue queue,
        IOptions<DbSyncProcessingOptions> options,
        ILogger<DbSyncBackgroundWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _queue = queue;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.ProcessingMode == DbSyncProcessingMode.RabbitMQ)
            return;

        _logger.LogInformation("DIP sync background worker started (in-process)");
        while (!stoppingToken.IsCancellationRequested)
        {
            if (_queue.TryDequeue(out var jobId))
            {
                try
                {
                    using var scope = _scopeFactory.CreateScope();
                    var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
                    accessor.BypassTenantFilter = true;
                    var orchestrator = scope.ServiceProvider.GetRequiredService<IDbSyncOrchestrator>();
                    await orchestrator.ProcessPendingJobAsync(jobId, stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "DIP sync job {JobId} failed", jobId);
                }
            }
            else
            {
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }
    }
}

public sealed class DbSyncRabbitWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DbSyncDispatcher _dispatcher;
    private readonly DbSyncProcessingOptions _options;
    private readonly ILogger<DbSyncRabbitWorker> _logger;

    public DbSyncRabbitWorker(
        IServiceScopeFactory scopeFactory,
        DbSyncDispatcher dispatcher,
        IOptions<DbSyncProcessingOptions> options,
        ILogger<DbSyncRabbitWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.ProcessingMode != DbSyncProcessingMode.RabbitMQ)
            return;

        _logger.LogInformation("DIP sync RabbitMQ worker started on {Queue}", _options.SyncQueueName);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _dispatcher.EnsureRabbitChannel();
                var channel = _dispatcher.GetChannel();
                if (channel == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                    continue;
                }

                var result = channel.BasicGet(_options.SyncQueueName, autoAck: false);
                if (result == null)
                {
                    await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
                    continue;
                }

                var message = JsonSerializer.Deserialize<DbSyncJobMessage>(Encoding.UTF8.GetString(result.Body.Span));
                if (message != null)
                {
                    using var scope = _scopeFactory.CreateScope();
                    var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
                    accessor.BypassTenantFilter = true;
                    var orchestrator = scope.ServiceProvider.GetRequiredService<IDbSyncOrchestrator>();
                    await orchestrator.ProcessPendingJobAsync(message.JobId, stoppingToken);
                }

                channel.BasicAck(result.DeliveryTag, false);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DIP sync RabbitMQ worker error");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }
    }
}

public sealed class DbSyncScheduledWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DbSyncScheduledWorker> _logger;

    public DbSyncScheduledWorker(IServiceScopeFactory scopeFactory, ILogger<DbSyncScheduledWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DIP sync scheduled worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var accessor = scope.ServiceProvider.GetRequiredService<AutonomusCRM.Application.Common.Tenancy.ICurrentTenantAccessor>();
                accessor.BypassTenantFilter = true;
                var service = scope.ServiceProvider.GetRequiredService<IDbSyncScheduleService>();
                await service.ProcessDueSchedulesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "DIP scheduled sync tick failed"); }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
