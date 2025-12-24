using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Infrastructure.Events.EventBus;

/// <summary>
/// Interfaz para el Event Intelligence Bus
/// </summary>
public interface IEventBus
{
    Task PublishAsync<T>(T domainEvent, CancellationToken cancellationToken = default) where T : IDomainEvent;
    Task SubscribeAsync<T>(Func<T, CancellationToken, Task> handler, CancellationToken cancellationToken = default) where T : IDomainEvent;
}

