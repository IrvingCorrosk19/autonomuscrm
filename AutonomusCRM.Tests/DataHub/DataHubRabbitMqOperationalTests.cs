using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using Testcontainers.RabbitMq;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("DataHubRabbitMqSerial")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubRabbitMq")]
public class DataHubRabbitMqOperationalTests : IAsyncLifetime
{
    private RabbitMqContainer? _container;
    private readonly PostgresTestFixture _pg;
    private string? _hostName;
    private int _port = 5672;
    private string _userName = "autonomus";
    private string _password = "autonomus123";
    private bool _brokerAvailable;
    private const string Queue = "datahub.import.jobs.ops";
    private const string Dlq = "datahub.import.jobs.ops.dlq";

    public DataHubRabbitMqOperationalTests(PostgresTestFixture pg) => _pg = pg;

    public async Task InitializeAsync()
    {
        _hostName = Environment.GetEnvironmentVariable("INTEGRATION_TEST_RABBITMQ_HOST") ?? "127.0.0.1";
        if (int.TryParse(Environment.GetEnvironmentVariable("INTEGRATION_TEST_RABBITMQ_PORT"), out var envPort))
            _port = envPort;
        _userName = Environment.GetEnvironmentVariable("INTEGRATION_TEST_RABBITMQ_USER") ?? "autonomus";
        _password = Environment.GetEnvironmentVariable("INTEGRATION_TEST_RABBITMQ_PASSWORD") ?? "autonomus123";

        if (await CanConnectAsync(_hostName, _port))
        {
            _brokerAvailable = true;
            PurgeTestQueues();
            return;
        }

        var forceTestcontainers = string.Equals(
            Environment.GetEnvironmentVariable("INTEGRATION_TEST_RABBITMQ_FORCE_TESTCONTAINERS"),
            "true",
            StringComparison.OrdinalIgnoreCase);
        if (!forceTestcontainers)
            return;

        try
        {
            _container = new RabbitMqBuilder().Build();
            await _container.StartAsync();
            _hostName = _container.Hostname;
            _port = _container.GetMappedPublicPort(5672);
            _userName = "guest";
            _password = "guest";
            _brokerAvailable = true;
            PurgeTestQueues();
        }
        catch
        {
            if (await CanConnectAsync(_hostName, _port))
            {
                _brokerAvailable = true;
                PurgeTestQueues();
            }
        }
    }

    public Task DisposeAsync()
    {
        if (_container != null)
            return _container.DisposeAsync().AsTask();
        return Task.CompletedTask;
    }

    [SkippableFact]
    public async Task PoisonMessage_RoutesToDlq()
    {
        IntegrationTestSkip.IfUnavailable(_pg.SkipReason);
        if (_brokerAvailable)
        {
            PrepareQueues();
            using var channel = CreateChannel();
            PublishRaw(channel, "not-json", 0);
            var outcome = await CreateWorker(new StubOrchestrator()).ProcessOneCycleAsync();
            Assert.Equal(DataHubRabbitConsumeOutcome.DeadLettered, outcome);
            AssertBrokerDlqHasMessage();
            return;
        }

        var memory = CreateMemoryChannel();
        EnqueueRaw(memory, "not-json", 0);
        var inMemoryOutcome = await CreateWorker(new StubOrchestrator()).ProcessOneCycleAsync(memory);
        Assert.Equal(DataHubRabbitConsumeOutcome.DeadLettered, inMemoryOutcome);
        AssertMemoryDlqHasMessage(memory);
    }

    [SkippableFact]
    public async Task TenantMismatch_RoutesToDlq()
    {
        IntegrationTestSkip.IfUnavailable(_pg.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = await SeedJobAsync(tenantId, DataHubJobStatus.ReadyToImport);

        if (_brokerAvailable)
        {
            PrepareQueues();
            using var channel = CreateChannel();
            PublishJob(channel, jobId, Guid.NewGuid(), 0);
            var outcome = await CreateWorker(new StubOrchestrator()).ProcessOneCycleAsync();
            Assert.Equal(DataHubRabbitConsumeOutcome.DeadLettered, outcome);
            AssertBrokerDlqHasMessage();
            return;
        }

        var memory = CreateMemoryChannel();
        EnqueueJob(memory, jobId, Guid.NewGuid(), 0);
        var inMemoryOutcome = await CreateWorker(new StubOrchestrator()).ProcessOneCycleAsync(memory);
        Assert.Equal(DataHubRabbitConsumeOutcome.DeadLettered, inMemoryOutcome);
        AssertMemoryDlqHasMessage(memory);
    }

    [SkippableFact]
    public async Task CompletedJob_DuplicateMessage_AckedWithoutReprocessing()
    {
        IntegrationTestSkip.IfUnavailable(_pg.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = await SeedJobAsync(tenantId, DataHubJobStatus.Completed);
        var orchestrator = new StubOrchestrator();

        if (_brokerAvailable)
        {
            PrepareQueues();
            using var channel = CreateChannel();
            PublishJob(channel, jobId, tenantId, 0);
            var outcome = await CreateWorker(orchestrator).ProcessOneCycleAsync();
            Assert.Equal(DataHubRabbitConsumeOutcome.Acked, outcome);
            Assert.Equal(0, orchestrator.ProcessCalls);
            return;
        }

        var memory = CreateMemoryChannel();
        EnqueueJob(memory, jobId, tenantId, 0);
        var inMemoryOutcome = await CreateWorker(orchestrator).ProcessOneCycleAsync(memory);
        Assert.Equal(DataHubRabbitConsumeOutcome.Acked, inMemoryOutcome);
        Assert.Equal(0, orchestrator.ProcessCalls);
    }

    [SkippableFact]
    public async Task ProcessingFailure_RetriesThenDlq()
    {
        IntegrationTestSkip.IfUnavailable(_pg.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = await SeedJobAsync(tenantId, DataHubJobStatus.ReadyToImport);
        var worker = CreateWorker(new ThrowingOrchestrator(), Options.Create(new DataHubProcessingOptions
        {
            ProcessingMode = DataHubProcessingMode.RabbitMQ,
            ImportQueueName = Queue,
            ImportDeadLetterQueueName = Dlq,
            MaxRetryAttempts = 2
        }));

        if (_brokerAvailable)
        {
            PrepareQueues();
            using var channel = CreateChannel();
            PublishJob(channel, jobId, tenantId, 0);
            Assert.Equal(DataHubRabbitConsumeOutcome.Acked, await worker.ProcessOneCycleAsync());
            Assert.Equal(DataHubRabbitConsumeOutcome.DeadLettered, await worker.ProcessOneCycleAsync());
            AssertBrokerDlqHasMessage();
            return;
        }

        var memory = CreateMemoryChannel();
        EnqueueJob(memory, jobId, tenantId, retry: 1);
        Assert.Equal(DataHubRabbitConsumeOutcome.DeadLettered, await worker.ProcessOneCycleAsync(memory));
        AssertMemoryDlqHasMessage(memory);
    }

    [SkippableFact]
    public async Task PublishConsume_Ack_ThroughDispatcherAndWorker()
    {
        IntegrationTestSkip.IfUnavailable(_pg.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = await SeedJobAsync(tenantId, DataHubJobStatus.ReadyToImport);
        var orchestrator = new StubOrchestrator();

        if (_brokerAvailable)
        {
            PrepareQueues();
            var dispatcher = CreateDispatcher();
            await dispatcher.EnqueueImportJobAsync(tenantId, jobId);
            var worker = CreateWorker(orchestrator, CreateOptions(), dispatcher);
            var outcome = await worker.ProcessOneCycleAsync();
            Assert.Equal(DataHubRabbitConsumeOutcome.Acked, outcome);
            Assert.Equal(1, orchestrator.ProcessCalls);
            return;
        }

        var memory = CreateMemoryChannel();
        EnqueueJob(memory, jobId, tenantId, 0);
        var inMemoryOutcome = await CreateWorker(orchestrator).ProcessOneCycleAsync(memory);
        Assert.Equal(DataHubRabbitConsumeOutcome.Acked, inMemoryOutcome);
        Assert.Equal(1, orchestrator.ProcessCalls);
    }

    [SkippableFact]
    public async Task WorkerRestart_ResumesAfterDispatcherReset()
    {
        IntegrationTestSkip.IfUnavailable(_pg.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = await SeedJobAsync(tenantId, DataHubJobStatus.ReadyToImport);
        var orchestrator = new StubOrchestrator();

        if (_brokerAvailable)
        {
            PrepareQueues();
            var dispatcher = CreateDispatcher();
            dispatcher.EnsureRabbitChannel();
            dispatcher.ResetConnection();
            dispatcher.EnsureRabbitChannel();
            var worker = CreateWorker(orchestrator, CreateOptions(), dispatcher);
            using var channel = CreateChannel();
            PublishJob(channel, jobId, tenantId, 0);
            var outcome = await worker.ProcessOneCycleAsync();
            Assert.Equal(DataHubRabbitConsumeOutcome.Acked, outcome);
            Assert.Equal(1, orchestrator.ProcessCalls);
            return;
        }

        var memory = CreateMemoryChannel();
        EnqueueJob(memory, jobId, tenantId, 0);
        var inMemoryOutcome = await CreateWorker(orchestrator).ProcessOneCycleAsync(memory);
        Assert.Equal(DataHubRabbitConsumeOutcome.Acked, inMemoryOutcome);
        Assert.Equal(1, orchestrator.ProcessCalls);
    }

    [SkippableFact]
    public async Task LockContention_NacksForRetry()
    {
        IntegrationTestSkip.IfUnavailable(_pg.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = await SeedJobAsync(tenantId, DataHubJobStatus.ReadyToImport);
        await using var lockDb = CreateIsolatedDb();
        var lockRepo = new DataHubRepository(lockDb);
        Assert.True(await lockRepo.TryAcquireJobProcessingLockAsync(jobId));
        try
        {
            if (_brokerAvailable)
            {
                PrepareQueues();
                using var channel = CreateChannel();
                PublishJob(channel, jobId, tenantId, 0);
                var outcome = await CreateWorker(new StubOrchestrator(), isolatedRepository: true).ProcessOneCycleAsync();
                Assert.Equal(DataHubRabbitConsumeOutcome.Nacked, outcome);
                return;
            }

            var memory = CreateMemoryChannel();
            EnqueueJob(memory, jobId, tenantId, 0);
            var inMemoryOutcome = await CreateWorker(new StubOrchestrator(), isolatedRepository: true).ProcessOneCycleAsync(memory);
            Assert.Equal(DataHubRabbitConsumeOutcome.Nacked, inMemoryOutcome);
        }
        finally
        {
            await lockRepo.ReleaseJobProcessingLockAsync(jobId);
        }
    }

    private static InMemoryRabbitConsumeChannel CreateMemoryChannel() => new(Dlq);

    private static void EnqueueRaw(InMemoryRabbitConsumeChannel memory, string payload, int retry)
    {
        var props = memory.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object?> { ["x-retry-count"] = retry };
        memory.Enqueue(InMemoryRabbitConsumeChannel.CreateGetResult(Queue, Encoding.UTF8.GetBytes(payload), props));
    }

    private static void EnqueueJob(InMemoryRabbitConsumeChannel memory, Guid jobId, Guid tenantId, int retry)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new DataHubImportJobMessage(jobId, tenantId));
        var props = memory.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Headers = new Dictionary<string, object?> { ["x-retry-count"] = retry };
        memory.Enqueue(InMemoryRabbitConsumeChannel.CreateGetResult(Queue, bytes, props));
    }

    private static void AssertMemoryDlqHasMessage(InMemoryRabbitConsumeChannel memory)
        => Assert.True(memory.DeadLetters.Count >= 1, $"Expected in-memory DLQ '{Dlq}' to contain a message.");

    private void AssertBrokerDlqHasMessage()
    {
        using var verify = CreateChannel();
        var info = verify.QueueDeclarePassive(Dlq);
        Assert.True(info.MessageCount >= 1, $"Expected DLQ '{Dlq}' to contain a message.");
        verify.BasicGet(Dlq, autoAck: true);
    }

    private void PrepareQueues()
    {
        using var channel = CreateChannel();
        channel.QueuePurge(Queue);
        channel.QueuePurge(Dlq);
    }

    private DataHubImportRabbitWorker CreateWorker(
        IDataHubOrchestrator orchestrator,
        IOptions<DataHubProcessingOptions>? options = null,
        DataHubImportDispatcher? dispatcher = null,
        bool isolatedRepository = false)
    {
        var services = new ServiceCollection();
        services.AddSingleton<ICurrentTenantAccessor>(new TestTenantAccessor { BypassTenantFilter = true });
        if (isolatedRepository)
            services.AddScoped<IDataHubRepository>(_ => new DataHubRepository(CreateIsolatedDb()));
        else
            services.AddScoped<IDataHubRepository>(_ => new DataHubRepository(_pg.Db!));
        services.AddScoped<IDataHubOrchestrator>(_ => orchestrator);
        var sp = services.BuildServiceProvider();
        return new DataHubImportRabbitWorker(
            sp.GetRequiredService<IServiceScopeFactory>(),
            dispatcher ?? CreateDispatcher(),
            options ?? CreateOptions(),
            NullLogger<DataHubImportRabbitWorker>.Instance,
            NullLogger<DataHubRabbitImportConsumer>.Instance);
    }

    private DataHubImportDispatcher CreateDispatcher() => new(
        new DataHubJobQueue(),
        CreateOptions(),
        Options.Create(new RabbitMQOptions { HostName = _hostName!, Port = _port, UserName = _userName, Password = _password }),
        NullLogger<DataHubImportDispatcher>.Instance);

    private IOptions<DataHubProcessingOptions> CreateOptions() => Options.Create(new DataHubProcessingOptions
    {
        ProcessingMode = DataHubProcessingMode.RabbitMQ,
        ImportQueueName = Queue,
        ImportDeadLetterQueueName = Dlq,
        MaxRetryAttempts = 3
    });

    private IModel CreateChannel()
    {
        var factory = new ConnectionFactory { HostName = _hostName!, Port = _port, UserName = _userName, Password = _password };
        var conn = factory.CreateConnection();
        var channel = conn.CreateModel();
        channel.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
        channel.QueueDeclare(Dlq, durable: true, exclusive: false, autoDelete: false);
        return channel;
    }

    private void PurgeTestQueues()
    {
        try
        {
            var factory = new ConnectionFactory { HostName = _hostName!, Port = _port, UserName = _userName, Password = _password };
            using var conn = factory.CreateConnection();
            using var channel = conn.CreateModel();
            channel.QueueDeclare(Queue, durable: true, exclusive: false, autoDelete: false);
            channel.QueueDeclare(Dlq, durable: true, exclusive: false, autoDelete: false);
            channel.QueuePurge(Queue);
            channel.QueuePurge(Dlq);
        }
        catch
        {
            // Best-effort purge before operational tests.
        }
    }

    private static void PublishRaw(IModel channel, string payload, int retry)
    {
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.Headers = new Dictionary<string, object> { ["x-retry-count"] = retry };
        channel.BasicPublish("", Queue, props, Encoding.UTF8.GetBytes(payload));
    }

    private static void PublishJob(IModel channel, Guid jobId, Guid tenantId, int retry)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(new DataHubImportJobMessage(jobId, tenantId));
        var props = channel.CreateBasicProperties();
        props.Persistent = true;
        props.ContentType = "application/json";
        props.Headers = new Dictionary<string, object> { ["x-retry-count"] = retry };
        channel.BasicPublish("", Queue, props, bytes);
    }

    private async Task<Guid> SeedJobAsync(Guid tenantId, DataHubJobStatus status)
    {
        var db = _pg.Db!;
        var jobId = Guid.NewGuid();
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            CreatedByUserId = Guid.NewGuid(),
            FileName = "ops.csv",
            TargetEntity = "Lead",
            Status = status.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        return jobId;
    }

    private DataHubRepository CreateRepository() => new(_pg.Db!);

    private ApplicationDbContext CreateIsolatedDb()
    {
        var accessor = new TestTenantAccessor { BypassTenantFilter = true };
        return new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_pg.ConnectionString!).Options,
            accessor);
    }

    private static async Task<bool> CanConnectAsync(string host, int port)
    {
        for (var i = 0; i < 5; i++)
        {
            try
            {
                using var c = new TcpClient();
                await c.ConnectAsync(host, port);
                return true;
            }
            catch when (i < 4)
            {
                await Task.Delay(1000);
            }
            catch
            {
                return false;
            }
        }

        return false;
    }

    private class StubOrchestrator : IDataHubOrchestrator
    {
        public int ProcessCalls;
        public virtual Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            ProcessCalls++;
            return Task.CompletedTask;
        }
        public Task<DataHubUploadResultDto> UploadAsync(Guid a, Guid b, Stream c, string d, string e, string f, bool g, CancellationToken h = default) => throw new NotImplementedException();
        public Task<DataHubAiAnalysisResultDto> AnalyzeWithAiAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task<DataHubAutoMapResult> AutoMapAsync(Guid a, Guid b, CancellationToken c = default)
            => Task.FromResult(new DataHubAutoMapResult(Array.Empty<DataHubMappingDto>(), 0, 0));
        public Task SaveMappingsAsync(Guid a, Guid b, IReadOnlyList<DataHubMappingDto> c, CancellationToken d = default) => Task.CompletedTask;
        public Task<DataHubValidationResultDto> ValidateAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task<DataHubExtendedValidationResultDto> ValidateExtendedAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task<DataHubCleaningSummaryDto> GetCleaningSummaryAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task<DataHubAutoFixResultDto> AutoFixAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task<DataHubImportResultDto> StartImportAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task CancelJobAsync(Guid a, Guid b, CancellationToken c = default) => Task.CompletedTask;
        public Task<DataHubImportResultDto> RetryFailedRowsAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task RecoverOrphanJobAsync(Guid a, CancellationToken b = default) => Task.CompletedTask;
        public Task RollbackJobAsync(Guid a, Guid b, int? c = null, int? d = null, CancellationToken e = default) => Task.CompletedTask;
        public Task<DataHubDuplicateScanResultDto> ScanDuplicatesAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task UpdateStagingRowsAsync(Guid a, Guid b, IReadOnlyList<DataHubStagingRowUpdateDto> c, CancellationToken d = default) => Task.CompletedTask;
        public Task<DataHubImportSummaryDto> GetImportSummaryAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
        public Task<DataHubTemplateSummaryDto> SaveTemplateFromJobAsync(Guid a, Guid b, string c, CancellationToken d = default) => throw new NotImplementedException();
        public Task<DataHubJobMetricsDto> GetJobMetricsAsync(Guid a, Guid b, CancellationToken c = default) => throw new NotImplementedException();
    }

    private sealed class ThrowingOrchestrator : StubOrchestrator
    {
        public override Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated failure");
    }
}
