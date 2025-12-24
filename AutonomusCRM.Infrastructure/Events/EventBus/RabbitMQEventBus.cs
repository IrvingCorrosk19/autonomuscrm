using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using System.Text;
using System.Text.Json;

namespace AutonomusCRM.Infrastructure.Events.EventBus;

/// <summary>
/// Implementación de Event Bus usando RabbitMQ
/// </summary>
public class RabbitMQEventBus : IEventBus, IDisposable
{
    private readonly IConnection _connection;
    private readonly IModel _channel;
    private readonly ILogger<RabbitMQEventBus> _logger;
    private readonly RabbitMQOptions _options;
    private readonly Dictionary<string, List<Func<IDomainEvent, CancellationToken, Task>>> _handlers = new();

    public RabbitMQEventBus(
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQEventBus> logger)
    {
        _options = options.Value;
        _logger = logger;

        var factory = new ConnectionFactory
        {
            HostName = _options.HostName,
            Port = _options.Port,
            UserName = _options.UserName,
            Password = _options.Password,
            VirtualHost = _options.VirtualHost ?? "/"
        };

        _connection = factory.CreateConnection();
        _channel = _connection.CreateModel();

        // Declarar exchange
        _channel.ExchangeDeclare(
            exchange: _options.ExchangeName ?? "autonomuscrm.events",
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);

        _logger.LogInformation("RabbitMQ Event Bus initialized");
    }

    public async Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        try
        {
            var message = JsonSerializer.Serialize(domainEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var routingKey = domainEvent.EventType.Replace(".", ".");

            _channel.BasicPublish(
                exchange: _options.ExchangeName ?? "autonomuscrm.events",
                routingKey: routingKey,
                basicProperties: null,
                body: body);

            _logger.LogInformation(
                "Published event {EventType} to RabbitMQ with routing key {RoutingKey}",
                domainEvent.EventType,
                routingKey);

            await Task.CompletedTask;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event to RabbitMQ");
            throw;
        }
    }

    public async Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        var eventType = typeof(T).Name;
        var queueName = $"{_options.QueuePrefix ?? "autonomuscrm"}.{eventType}";

        // Declarar cola
        _channel.QueueDeclare(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null);

        // Bind cola al exchange
        var routingKey = eventType.Replace(".", ".");
        _channel.QueueBind(
            queue: queueName,
            exchange: _options.ExchangeName ?? "autonomuscrm.events",
            routingKey: routingKey);

        // Configurar consumer
        var consumer = new RabbitMQ.Client.Events.EventingBasicConsumer(_channel);
        consumer.Received += async (model, ea) =>
        {
            try
            {
                var body = ea.Body.ToArray();
                var message = Encoding.UTF8.GetString(body);
                var domainEvent = JsonSerializer.Deserialize<T>(message);

                if (domainEvent != null)
                {
                    await handler(domainEvent, cancellationToken);
                    _channel.BasicAck(ea.DeliveryTag, false);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing event from RabbitMQ");
                _channel.BasicNack(ea.DeliveryTag, false, true); // Requeue
            }
        };

        _channel.BasicConsume(
            queue: queueName,
            autoAck: false,
            consumer: consumer);

        _logger.LogInformation("Subscribed to event {EventType} on queue {QueueName}", eventType, queueName);

        await Task.CompletedTask;
    }

    public void Dispose()
    {
        _channel?.Close();
        _connection?.Close();
        _channel?.Dispose();
        _connection?.Dispose();
    }
}

public class RabbitMQOptions
{
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string? UserName { get; set; }
    public string? Password { get; set; }
    public string? VirtualHost { get; set; }
    public string? ExchangeName { get; set; } = "autonomuscrm.events";
    public string? QueuePrefix { get; set; } = "autonomuscrm";
}

