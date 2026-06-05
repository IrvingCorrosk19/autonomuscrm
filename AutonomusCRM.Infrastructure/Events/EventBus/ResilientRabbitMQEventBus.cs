using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Infrastructure.Caching;
using AutonomusCRM.Infrastructure.Events;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Infrastructure.Persistence.EventStore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;

namespace AutonomusCRM.Infrastructure.Events.EventBus;

/// <summary>
/// Event bus RabbitMQ con reconexión, DLX, idempotencia y persistencia de poison messages.
/// </summary>
public sealed class ResilientRabbitMQEventBus : IEventBus, IDisposable
{
    private static readonly ActivitySource ActivitySource = new("AutonomusCRM.EventBus");

    private readonly RabbitMQOptions _options;
    private readonly ILogger<ResilientRabbitMQEventBus> _logger;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly object _sync = new();
    private IConnection? _connection;
    private IModel? _channel;
    private const int MaxDeliveryAttempts = 3;

    public ResilientRabbitMQEventBus(
        IOptions<RabbitMQOptions> options,
        ILogger<ResilientRabbitMQEventBus> logger,
        IServiceScopeFactory scopeFactory)
    {
        _options = options.Value;
        _logger = logger;
        _scopeFactory = scopeFactory;
    }

    private void EnsureConnected()
    {
        lock (_sync)
        {
            if (_connection?.IsOpen == true && _channel?.IsOpen == true)
                return;

            var factory = new ConnectionFactory
            {
                HostName = _options.HostName,
                Port = _options.Port,
                UserName = _options.UserName,
                Password = _options.Password,
                VirtualHost = _options.VirtualHost ?? "/",
                AutomaticRecoveryEnabled = true,
                NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                DispatchConsumersAsync = true
            };

            _connection?.Dispose();
            _channel?.Dispose();

            Exception? last = null;
            for (var attempt = 1; attempt <= 10; attempt++)
            {
                try
                {
                    _connection = factory.CreateConnection();
                    _channel = _connection.CreateModel();
                    last = null;
                    break;
                }
                catch (Exception ex)
                {
                    last = ex;
                    _logger.LogWarning(ex, "RabbitMQ connect attempt {Attempt} failed", attempt);
                    Thread.Sleep(TimeSpan.FromSeconds(Math.Min(2 * attempt, 15)));
                }
            }

            if (last is not null || _channel is null)
                throw new BrokerUnreachableException(last);

            var exchange = _options.ExchangeName ?? "autonomuscrm.events";
            var dlx = $"{exchange}.dlx";

            _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.ExchangeDeclare(dlx, ExchangeType.Topic, durable: true, autoDelete: false);

            _logger.LogInformation("RabbitMQ connected to {Host}:{Port}", _options.HostName, _options.Port);
        }
    }

    private IModel RequireChannel()
    {
        EnsureConnected();
        return _channel ?? throw new InvalidOperationException("RabbitMQ channel is not connected.");
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        using var activity = ActivitySource.StartActivity("rabbitmq.publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", domainEvent.EventType);

        try
        {
            var channel = RequireChannel();
            var message = JsonSerializer.Serialize(domainEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var props = channel.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = domainEvent.Id.ToString();
            props.CorrelationId = domainEvent.CorrelationId?.ToString();
            props.Headers = new Dictionary<string, object>
            {
                ["event-type"] = domainEvent.EventType,
                ["tenant-id"] = domainEvent.TenantId?.ToString() ?? string.Empty
            };

            channel.BasicPublish(
                exchange: _options.ExchangeName ?? "autonomuscrm.events",
                routingKey: domainEvent.EventType,
                mandatory: false,
                basicProperties: props,
                body: body);

            _logger.LogInformation(
                "Published {EventType} MessageId={MessageId}",
                domainEvent.EventType,
                props.MessageId);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Publish failed for {EventType}", domainEvent.EventType);
            throw;
        }
    }

    public async Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        var channel = RequireChannel();
        var routingKey = DomainEventRouting.GetRoutingKey<T>();
        var exchange = _options.ExchangeName ?? "autonomuscrm.events";
        var dlx = $"{exchange}.dlx";
        var queueName = $"{_options.QueuePrefix ?? "autonomuscrm"}.{routingKey.Replace('.', '_')}";
        var dlqName = $"{queueName}.dlq";

        RabbitMqQueueHelper.DeclareMainQueue(
            channel, queueName, exchange, routingKey, dlx, _logger);
        RabbitMqQueueHelper.DeclareDlq(channel, dlqName, dlx, $"{routingKey}.failed");

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.Received += async (_, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString();
            var idempotencyKey = $"evt:processed:{messageId}";

            using (var idempotencyScope = _scopeFactory.CreateScope())
            {
                var cache = idempotencyScope.ServiceProvider.GetRequiredService<ICacheService>();
                if (await cache.GetAsync<string>(idempotencyKey, cancellationToken) != null)
                {
                    channel.BasicAck(ea.DeliveryTag, false);
                    return;
                }
            }

            using var activity = ActivitySource.StartActivity("rabbitmq.consume", ActivityKind.Consumer);
            var body = Encoding.UTF8.GetString(ea.Body.ToArray());

            try
            {
                IDomainEvent? domainEvent = null;
                using (var doc = JsonDocument.Parse(body))
                {
                    if (doc.RootElement.TryGetProperty("EventType", out var etProp))
                    {
                        var eventType = etProp.GetString();
                        if (!string.IsNullOrEmpty(eventType) &&
                            DomainEventTypeRegistry.TryDeserialize(eventType, body, out var deserialized))
                            domainEvent = deserialized;
                    }
                }

                domainEvent ??= TryMaterializeKnownEvent(body);
                domainEvent ??= JsonSerializer.Deserialize<T>(body);

                if (domainEvent is not T typedEvent)
                {
                    channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                await handler(typedEvent, cancellationToken);
                using (var idempotencyScope = _scopeFactory.CreateScope())
                {
                    var cache = idempotencyScope.ServiceProvider.GetRequiredService<ICacheService>();
                    await cache.SetAsync(idempotencyKey, "1", TimeSpan.FromDays(7), cancellationToken);
                }
                channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var retryKey = $"evt:retry:{messageId}";
                var retryCount = 0;
                using (var retryScope = _scopeFactory.CreateScope())
                {
                    var cache = retryScope.ServiceProvider.GetRequiredService<ICacheService>();
                    var existing = await cache.GetAsync<RetryEnvelope>(retryKey, cancellationToken);
                    retryCount = (existing?.Count ?? 0) + 1;
                    await cache.SetAsync(retryKey, new RetryEnvelope(retryCount), TimeSpan.FromHours(1), cancellationToken);
                }

                _logger.LogError(ex, "Consume failed MessageId={MessageId} Retry={Retry}", messageId, retryCount);

                if (retryCount >= MaxDeliveryAttempts)
                {
                    await PersistPoisonMessageAsync(body, routingKey, messageId, ex, ea, cancellationToken);
                    channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    channel.BasicNack(ea.DeliveryTag, false, true);
                }
            }
        };

        channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogInformation("Subscribed {RoutingKey} queue={Queue}", routingKey, queueName);
        await Task.CompletedTask;
    }

    private static IDomainEvent? TryMaterializeKnownEvent(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (!doc.RootElement.TryGetProperty("EventType", out var et))
                return null;
            var eventType = et.GetString();
            if (string.IsNullOrWhiteSpace(eventType))
                return null;

            var tenantId = doc.RootElement.TryGetProperty("TenantId", out var tid) && tid.TryGetGuid(out var parsedTid)
                ? parsedTid
                : Guid.Empty;

            return eventType switch
            {
                "Lead.Created" when doc.RootElement.TryGetProperty("LeadId", out var leadId) &&
                                    doc.RootElement.TryGetProperty("LeadName", out var leadName) &&
                                    doc.RootElement.TryGetProperty("Source", out var leadSource) &&
                                    leadId.TryGetGuid(out var lid)
                    => new LeadCreatedEvent(lid, tenantId, leadName.GetString() ?? "Lead", (LeadSource)leadSource.GetInt32()),

                "Customer.Created" when doc.RootElement.TryGetProperty("CustomerId", out var customerId) &&
                                        doc.RootElement.TryGetProperty("CustomerName", out var customerName) &&
                                        customerId.TryGetGuid(out var cid)
                    => new CustomerCreatedEvent(cid, tenantId, customerName.GetString() ?? "Customer"),

                "Deal.Created" when doc.RootElement.TryGetProperty("DealId", out var dealId) &&
                                    doc.RootElement.TryGetProperty("CustomerId", out var dealCustomerId) &&
                                    doc.RootElement.TryGetProperty("Title", out var title) &&
                                    doc.RootElement.TryGetProperty("Amount", out var amount) &&
                                    dealId.TryGetGuid(out var did) &&
                                    dealCustomerId.TryGetGuid(out var dcid)
                    => new DealCreatedEvent(did, tenantId, dcid, title.GetString() ?? "Deal", amount.GetDecimal()),

                "Deal.StageChanged" when doc.RootElement.TryGetProperty("DealId", out var stageDealId) &&
                                         doc.RootElement.TryGetProperty("OldStage", out var oldStage) &&
                                         doc.RootElement.TryGetProperty("NewStage", out var newStage) &&
                                         doc.RootElement.TryGetProperty("Probability", out var probability) &&
                                         stageDealId.TryGetGuid(out var sdid)
                    => new DealStageChangedEvent(sdid, tenantId, (DealStage)oldStage.GetInt32(), (DealStage)newStage.GetInt32(), probability.GetInt32()),

                _ => null
            };
        }
        catch
        {
            return null;
        }
    }

    private sealed record RetryEnvelope(int Count);

    private async Task PersistPoisonMessageAsync(
        string body,
        string routingKey,
        string messageId,
        Exception ex,
        BasicDeliverEventArgs ea,
        CancellationToken cancellationToken)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
            accessor.BypassTenantFilter = true;

            Guid? tenantId = null;
            if (ea.BasicProperties.Headers?.TryGetValue("tenant-id", out var tid) == true)
            {
                var tidStr = Encoding.UTF8.GetString((byte[])tid);
                if (Guid.TryParse(tidStr, out var parsed))
                    tenantId = parsed;
            }

            var eventType = routingKey;
            try
            {
                using var doc = JsonDocument.Parse(body);
                if (doc.RootElement.TryGetProperty("EventType", out var et))
                    eventType = et.GetString() ?? routingKey;
            }
            catch { /* ignore */ }

            db.FailedEventMessages.Add(new FailedEventMessage
            {
                MessageId = messageId,
                TenantId = tenantId,
                EventType = eventType,
                RoutingKey = routingKey,
                Payload = body,
                Error = ex.Message,
                RetryCount = MaxDeliveryAttempts
            });
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception persistEx)
        {
            _logger.LogCritical(persistEx, "Failed to persist poison message {MessageId}", messageId);
        }
    }

    public void Dispose()
    {
        _channel?.Dispose();
        _connection?.Dispose();
    }
}
