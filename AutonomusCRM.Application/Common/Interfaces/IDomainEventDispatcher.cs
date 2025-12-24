using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.Common.Interfaces;

/// <summary>
/// Interfaz para despachar eventos de dominio
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default);
}

