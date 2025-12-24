using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AutonomusCRM.Infrastructure.Events.EventBus;

/// <summary>
/// Implementación en memoria del Event Bus (para desarrollo)
/// En producción usar RabbitMQ, Azure Service Bus, etc.
/// </summary>
public class InMemoryEventBus : IEventBus
{
    private readonly ConcurrentDictionary<Type, List<Func<IDomainEvent, CancellationToken, Task>>> _handlers = new();
    private readonly ILogger<InMemoryEventBus> _logger;

    public InMemoryEventBus(ILogger<InMemoryEventBus> logger)
    {
        _logger = logger;
    }

    public Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        _logger.LogInformation(
            "Publishing event: {EventType} (Id: {EventId}, TenantId: {TenantId})",
            domainEvent.EventType,
            domainEvent.Id,
            domainEvent.TenantId);

        var eventType = typeof(T);
        if (_handlers.TryGetValue(eventType, out var handlers))
        {
            var tasks = handlers.Select(handler => handler(domainEvent, cancellationToken));
            return Task.WhenAll(tasks);
        }

        return Task.CompletedTask;
    }

    public Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : IDomainEvent
    {
        var eventType = typeof(T);
        var handlers = _handlers.GetOrAdd(eventType, _ => new List<Func<IDomainEvent, CancellationToken, Task>>());
        
        handlers.Add((domainEvent, ct) => handler((T)domainEvent, ct));
        
        _logger.LogInformation("Subscribed to event type: {EventType}", eventType.Name);
        
        return Task.CompletedTask;
    }
}

