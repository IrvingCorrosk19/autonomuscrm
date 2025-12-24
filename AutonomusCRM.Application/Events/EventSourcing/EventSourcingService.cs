using AutonomusCRM.Domain.Common;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Events.EventSourcing;

/// <summary>
/// Servicio para reconstruir agregados desde eventos (Event Sourcing)
/// </summary>
public interface IEventSourcingService
{
    Task<T?> ReconstructAggregateAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : AggregateRoot;
    Task SaveSnapshotAsync<T>(T aggregate, CancellationToken cancellationToken = default) where T : AggregateRoot;
    Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = 0, CancellationToken cancellationToken = default);
}

public class EventSourcingService : IEventSourcingService
{
    private readonly IEventStore _eventStore;
    private readonly ISnapshotStore _snapshotStore;
    private readonly ILogger<EventSourcingService> _logger;

    public EventSourcingService(
        IEventStore eventStore,
        ISnapshotStore snapshotStore,
        ILogger<EventSourcingService> logger)
    {
        _eventStore = eventStore;
        _snapshotStore = snapshotStore;
        _logger = logger;
    }

    public async Task<T?> ReconstructAggregateAsync<T>(Guid aggregateId, CancellationToken cancellationToken = default) where T : AggregateRoot
    {
        // Intentar obtener snapshot
        var snapshotVersion = await _snapshotStore.GetSnapshotVersionAsync(aggregateId, cancellationToken);
        
        // Obtener eventos desde la versión del snapshot (o desde el inicio)
        var events = await GetEventsAsync(aggregateId, snapshotVersion ?? 0, cancellationToken);

        if (!events.Any())
        {
            // Si hay snapshot pero no eventos nuevos, intentar reconstruir desde snapshot
            if (snapshotVersion.HasValue)
            {
                var snapshot = await _snapshotStore.GetSnapshotAsync<T>(aggregateId, cancellationToken);
                if (snapshot != null)
                {
                    _logger.LogInformation("Reconstructed aggregate {AggregateId} from snapshot at version {Version}", aggregateId, snapshotVersion);
                    return snapshot;
                }
            }
            return null;
        }

        // Reconstruir desde snapshot + eventos
        T? aggregate = null;
        if (snapshotVersion.HasValue)
        {
            aggregate = await _snapshotStore.GetSnapshotAsync<T>(aggregateId, cancellationToken);
        }

        // Si no hay snapshot, crear nueva instancia
        if (aggregate == null)
        {
            // TODO: Usar reflection o factory para crear instancia
            // Por ahora, esto requiere implementación específica por tipo
            _logger.LogWarning("Cannot reconstruct aggregate {AggregateId} without snapshot or factory", aggregateId);
            return null;
        }

        // Aplicar eventos
        foreach (var domainEvent in events)
        {
            // TODO: Aplicar evento al agregado usando reflection o método Apply
            // aggregate.Apply(domainEvent);
        }

        _logger.LogInformation("Reconstructed aggregate {AggregateId} from {EventCount} events", aggregateId, events.Count);
        return aggregate;
    }

    public async Task SaveSnapshotAsync<T>(T aggregate, CancellationToken cancellationToken = default) where T : AggregateRoot
    {
        // Determinar versión basada en eventos
        var events = await GetEventsAsync(aggregate.Id, cancellationToken: cancellationToken);
        var version = events.Count;

        await _snapshotStore.SaveSnapshotAsync(aggregate.Id, aggregate, version, cancellationToken);
        _logger.LogInformation("Saved snapshot for aggregate {AggregateId} at version {Version}", aggregate.Id, version);
    }

    public async Task<List<IDomainEvent>> GetEventsAsync(Guid aggregateId, int fromVersion = 0, CancellationToken cancellationToken = default)
    {
        return await _eventStore.GetEventsByAggregateIdAsync(aggregateId, fromVersion, cancellationToken);
    }
}

