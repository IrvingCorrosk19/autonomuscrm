using System.Diagnostics;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Domain.Events;
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
        EnsureConnected();
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
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            var exchange = _options.ExchangeName ?? "autonomuscrm.events";
            var dlx = $"{exchange}.dlx";

            _channel.ExchangeDeclare(exchange, ExchangeType.Topic, durable: true, autoDelete: false);
            _channel.ExchangeDeclare(dlx, ExchangeType.Topic, durable: true, autoDelete: false);

            _logger.LogInformation("RabbitMQ connected to {Host}:{Port}", _options.HostName, _options.Port);
        }
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        using var activity = ActivitySource.StartActivity("rabbitmq.publish", ActivityKind.Producer);
        activity?.SetTag("messaging.system", "rabbitmq");
        activity?.SetTag("messaging.destination", domainEvent.EventType);

        try
        {
            EnsureConnected();
            var message = JsonSerializer.Serialize(domainEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var props = _channel!.CreateBasicProperties();
            props.Persistent = true;
            props.MessageId = domainEvent.Id.ToString();
            props.CorrelationId = domainEvent.CorrelationId?.ToString();
            props.Headers = new Dictionary<string, object>
            {
                ["event-type"] = domainEvent.EventType,
                ["tenant-id"] = domainEvent.TenantId?.ToString() ?? string.Empty
            };

            _channel.BasicPublish(
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
        EnsureConnected();
        var routingKey = DomainEventRouting.GetRoutingKey<T>();
        var exchange = _options.ExchangeName ?? "autonomuscrm.events";
        var dlx = $"{exchange}.dlx";
        var queueName = $"{_options.QueuePrefix ?? "autonomuscrm"}.{routingKey.Replace('.', '_')}";
        var dlqName = $"{queueName}.dlq";

        var args = new Dictionary<string, object>
        {
            ["x-dead-letter-exchange"] = dlx,
            ["x-dead-letter-routing-key"] = $"{routingKey}.failed"
        };

        _channel!.QueueDeclare(queueName, durable: true, exclusive: false, autoDelete: false, arguments: args);
        _channel.QueueDeclare(dlqName, durable: true, exclusive: false, autoDelete: false);
        _channel.QueueBind(queueName, exchange, routingKey);
        _channel.QueueBind(dlqName, dlx, $"{routingKey}.failed");

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.Received += async (_, ea) =>
        {
            var messageId = ea.BasicProperties.MessageId ?? ea.DeliveryTag.ToString();
            var idempotencyKey = $"evt:processed:{messageId}";

            using (var idempotencyScope = _scopeFactory.CreateScope())
            {
                var cache = idempotencyScope.ServiceProvider.GetRequiredService<ICacheService>();
                if (await cache.GetAsync<string>(idempotencyKey, cancellationToken) != null)
                {
                    _channel.BasicAck(ea.DeliveryTag, false);
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

                domainEvent ??= JsonSerializer.Deserialize<T>(body);

                if (domainEvent is not T typedEvent)
                {
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                    return;
                }

                await handler(typedEvent, cancellationToken);
                using (var idempotencyScope = _scopeFactory.CreateScope())
                {
                    var cache = idempotencyScope.ServiceProvider.GetRequiredService<ICacheService>();
                    await cache.SetAsync(idempotencyKey, "1", TimeSpan.FromDays(7), cancellationToken);
                }
                _channel.BasicAck(ea.DeliveryTag, false);
            }
            catch (Exception ex)
            {
                var retryHeader = "x-retry-count";
                var retryCount = 0;
                if (ea.BasicProperties.Headers?.TryGetValue(retryHeader, out var rc) == true && rc is int i)
                    retryCount = i;

                retryCount++;
                _logger.LogError(ex, "Consume failed MessageId={MessageId} Retry={Retry}", messageId, retryCount);

                if (retryCount >= MaxDeliveryAttempts)
                {
                    await PersistPoisonMessageAsync(body, routingKey, messageId, ex, ea, cancellationToken);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                else
                {
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            }
        };

        _channel.BasicConsume(queueName, autoAck: false, consumer);
        _logger.LogInformation("Subscribed {RoutingKey} queue={Queue}", routingKey, queueName);
        await Task.CompletedTask;
    }

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
