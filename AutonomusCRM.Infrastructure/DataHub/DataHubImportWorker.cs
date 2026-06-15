using System.Runtime.CompilerServices;
using System.Text;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text.Json;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubProcessingOptions
{
    public DataHubProcessingMode ProcessingMode { get; set; } = DataHubProcessingMode.InProcess;
    public string ImportQueueName { get; set; } = "datahub.import.jobs";
    public string ImportDeadLetterQueueName { get; set; } = "datahub.import.jobs.dlq";
    public int MaxRetryAttempts { get; set; } = 3;
}

public sealed class DataHubImportDispatcher : IDataHubImportDispatcher
{
    private readonly IDataHubJobQueue _inProcessQueue;
    private readonly DataHubProcessingOptions _options;
    private readonly RabbitMQOptions _rabbitOptions;
    private readonly ILogger<DataHubImportDispatcher> _logger;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;

    public DataHubImportDispatcher(
        IDataHubJobQueue inProcessQueue,
        IOptions<DataHubProcessingOptions> options,
        IOptions<RabbitMQOptions> rabbitOptions,
        ILogger<DataHubImportDispatcher> logger)
    {
        _inProcessQueue = inProcessQueue;
        _options = options.Value;
        _rabbitOptions = rabbitOptions.Value;
        _logger = logger;
    }

    public Task EnqueueImportJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        if (_options.ProcessingMode == DataHubProcessingMode.RabbitMQ)
        {
            if (!TryPublishRabbit(tenantId, jobId))
                throw new InvalidOperationException(
                    $"RabbitMQ dispatch failed for job {jobId}; in-process fallback is disabled in RabbitMQ mode.");
            return Task.CompletedTask;
        }

        _inProcessQueue.Enqueue(jobId);
        return Task.CompletedTask;
    }

    private bool TryPublishRabbit(Guid tenantId, Guid jobId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(_rabbitOptions.HostName)) return false;
            EnsureRabbitChannel();
            var payload = JsonSerializer.SerializeToUtf8Bytes(new DataHubImportJobMessage(jobId, tenantId));
            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.ContentType = "application/json";
            props.Headers = new Dictionary<string, object> { ["x-retry-count"] = 0 };
            _channel.BasicPublish("", _options.ImportQueueName, props, payload);
            _logger.LogInformation("DataHub job {JobId} dispatched to RabbitMQ queue {Queue}", jobId, _options.ImportQueueName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "RabbitMQ dispatch failed for job {JobId}", jobId);
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
            _channel.QueueDeclare(_options.ImportQueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueDeclare(_options.ImportDeadLetterQueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.BasicQos(0, 1, false);
        }
    }

    internal IModel? GetChannel() => _channel;

    internal void ResetConnection()
    {
        lock (_sync)
        {
            _channel?.Dispose();
            _connection?.Dispose();
            _channel = null;
            _connection = null;
        }
    }
}

public enum DataHubRabbitConsumeOutcome
{
    Idle,
    Acked,
    Nacked,
    DeadLettered
}

public sealed class DataHubRabbitImportConsumer
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly DataHubImportDispatcher _dispatcher;
    private readonly DataHubProcessingOptions _options;
    private readonly ILogger<DataHubRabbitImportConsumer> _logger;

    public DataHubRabbitImportConsumer(
        IServiceScopeFactory scopeFactory,
        DataHubImportDispatcher dispatcher,
        IOptions<DataHubProcessingOptions> options,
        ILogger<DataHubRabbitImportConsumer> logger)
    {
        _scopeFactory = scopeFactory;
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
    }

    public Task<DataHubRabbitConsumeOutcome> ProcessNextAsync(IModel channel, CancellationToken cancellationToken)
        => ProcessNextAsync(new RabbitModelConsumeAdapter(channel), cancellationToken);

    internal async Task<DataHubRabbitConsumeOutcome> ProcessNextAsync(IRabbitConsumeChannel channel, CancellationToken cancellationToken)
    {
        var result = channel.BasicGet(_options.ImportQueueName, autoAck: false);
        if (result == null) return DataHubRabbitConsumeOutcome.Idle;

        DataHubImportJobMessage? msg;
        try
        {
            msg = JsonSerializer.Deserialize<DataHubImportJobMessage>(result.Body.Span);
        }
        catch (JsonException)
        {
            msg = null;
        }

        if (msg == null)
        {
            PublishToDeadLetter(channel, result.Body, "InvalidMessage");
            channel.BasicAck(result.DeliveryTag, false);
            return DataHubRabbitConsumeOutcome.DeadLettered;
        }

        var retryCount = GetRetryCount(result.BasicProperties);
        using var scope = _scopeFactory.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<Application.Common.Tenancy.ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var repo = scope.ServiceProvider.GetRequiredService<IDataHubRepository>();
        var job = await repo.GetJobByIdAsync(msg.JobId, cancellationToken);
        if (job == null || job.TenantId != msg.TenantId)
        {
            _logger.LogWarning("Rejected DataHub job {JobId}: tenant mismatch or not found", msg.JobId);
            PublishToDeadLetter(channel, result.Body, "TenantMismatchOrMissing");
            channel.BasicAck(result.DeliveryTag, false);
            return DataHubRabbitConsumeOutcome.DeadLettered;
        }

        if (job.Status is nameof(DataHubJobStatus.Completed) or nameof(DataHubJobStatus.CompletedWithErrors))
        {
            channel.BasicAck(result.DeliveryTag, false);
            return DataHubRabbitConsumeOutcome.Acked;
        }

        if (!await repo.TryAcquireJobProcessingLockAsync(msg.JobId, cancellationToken))
        {
            channel.BasicNack(result.DeliveryTag, false, true);
            return DataHubRabbitConsumeOutcome.Nacked;
        }

        try
        {
            var orchestrator = scope.ServiceProvider.GetRequiredService<IDataHubOrchestrator>();
            if (orchestrator is DataHubOrchestrator concrete)
                await concrete.ProcessJobCoreAsync(msg.JobId, acquireLock: false, cancellationToken);
            else
                await orchestrator.ProcessJobAsync(msg.JobId, cancellationToken);

            channel.BasicAck(result.DeliveryTag, false);
            _logger.LogInformation("DataHub import job {JobId} completed via RabbitMQ consumer", msg.JobId);
            return DataHubRabbitConsumeOutcome.Acked;
        }
        catch (Exception ex)
        {
            retryCount++;
            if (retryCount >= _options.MaxRetryAttempts)
            {
                PublishToDeadLetter(channel, result.Body, ex.Message);
                channel.BasicAck(result.DeliveryTag, false);
                _logger.LogError(ex, "DataHub job {JobId} moved to DLQ after {Retries} retries", msg.JobId, retryCount);
                return DataHubRabbitConsumeOutcome.DeadLettered;
            }

            RepublishWithRetry(msg, retryCount);
            channel.BasicAck(result.DeliveryTag, false);
            _logger.LogWarning(ex, "DataHub job {JobId} retry {Retry}/{Max}", msg.JobId, retryCount, _options.MaxRetryAttempts);
            return DataHubRabbitConsumeOutcome.Acked;
        }
        finally
        {
            await repo.ReleaseJobProcessingLockAsync(msg.JobId, cancellationToken);
        }
    }

    internal void RepublishWithRetry(DataHubImportJobMessage msg, int retryCount)
    {
        _dispatcher.EnsureRabbitChannel();
        var channel = _dispatcher.GetChannel();
        if (channel == null) return;
        var payload = JsonSerializer.SerializeToUtf8Bytes(msg);
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Headers = new Dictionary<string, object> { ["x-retry-count"] = retryCount };
        channel.BasicPublish("", _options.ImportQueueName, props, payload);
    }

    internal void PublishToDeadLetter(IRabbitConsumeChannel channel, ReadOnlyMemory<byte> body, string reason)
    {
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object> { ["x-death-reason"] = reason };
        channel.BasicPublish("", _options.ImportDeadLetterQueueName, props, body);
    }

    internal static int GetRetryCount(IBasicProperties? props)
    {
        if (props?.Headers == null || !props.Headers.TryGetValue("x-retry-count", out var val)) return 0;
        return val switch
        {
            int i => i,
            long l => (int)l,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), out var n) => n,
            _ => 0
        };
    }
}

public sealed class DataHubImportRabbitWorker : BackgroundService
{
    private readonly DataHubRabbitImportConsumer _consumer;
    private readonly DataHubImportDispatcher _dispatcher;
    private readonly DataHubProcessingOptions _options;
    private readonly ILogger<DataHubImportRabbitWorker> _logger;

    public DataHubImportRabbitWorker(
        IServiceScopeFactory scopeFactory,
        DataHubImportDispatcher dispatcher,
        IOptions<DataHubProcessingOptions> options,
        ILogger<DataHubImportRabbitWorker> logger,
        ILogger<DataHubRabbitImportConsumer> consumerLogger)
    {
        _dispatcher = dispatcher;
        _options = options.Value;
        _logger = logger;
        _consumer = new DataHubRabbitImportConsumer(scopeFactory, dispatcher, options, consumerLogger);
    }

    internal DataHubRabbitImportConsumer Consumer => _consumer;

    internal Task<DataHubRabbitConsumeOutcome> ProcessOneCycleAsync(CancellationToken cancellationToken = default)
        => ProcessOneCycleAsync(null, cancellationToken);

    internal Task<DataHubRabbitConsumeOutcome> ProcessOneCycleAsync(
        IRabbitConsumeChannel? consumeChannel,
        CancellationToken cancellationToken = default)
    {
        if (consumeChannel != null)
            return _consumer.ProcessNextAsync(consumeChannel, cancellationToken);

        _dispatcher.EnsureRabbitChannel();
        var channel = _dispatcher.GetChannel();
        if (channel == null) return Task.FromResult(DataHubRabbitConsumeOutcome.Idle);
        return _consumer.ProcessNextAsync(channel, cancellationToken);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.ProcessingMode != DataHubProcessingMode.RabbitMQ)
        {
            _logger.LogInformation("DataHub RabbitMQ worker disabled (ProcessingMode=InProcess)");
            return;
        }

        _logger.LogInformation("DataHub RabbitMQ import worker starting on queue {Queue}", _options.ImportQueueName);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                _dispatcher.EnsureRabbitChannel();
                var channel = _dispatcher.GetChannel();
                if (channel == null)
                {
                    await Task.Delay(5000, stoppingToken);
                    continue;
                }

                var outcome = await _consumer.ProcessNextAsync(channel, stoppingToken);
                if (outcome == DataHubRabbitConsumeOutcome.Idle)
                    await Task.Delay(500, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DataHub RabbitMQ worker error");
                await Task.Delay(3000, stoppingToken);
            }
        }
    }
}

public sealed class DataHubOrphanRecoveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataHubOrphanRecoveryWorker> _logger;

    public DataHubOrphanRecoveryWorker(IServiceScopeFactory scopeFactory, ILogger<DataHubOrphanRecoveryWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var accessor = scope.ServiceProvider.GetRequiredService<Application.Common.Tenancy.ICurrentTenantAccessor>();
                accessor.BypassTenantFilter = true;
                var repo = scope.ServiceProvider.GetRequiredService<IDataHubRepository>();
                var orphans = await repo.GetPendingJobsAsync(50, stoppingToken);
                foreach (var job in orphans.Where(j => j.Status == DataHubJobStatus.Importing.ToString()
                    && j.StartedAt.HasValue && DateTime.UtcNow - j.StartedAt.Value > TimeSpan.FromMinutes(10)))
                {
                    _logger.LogWarning("Recovering orphan DataHub job {JobId} tenant {TenantId}", job.Id, job.TenantId);
                    var orchestrator = scope.ServiceProvider.GetRequiredService<IDataHubOrchestrator>();
                    await orchestrator.RecoverOrphanJobAsync(job.Id, stoppingToken);
                }
            }
            catch (Exception ex) { _logger.LogError(ex, "DataHub orphan recovery error"); }

            await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
        }
    }
}
