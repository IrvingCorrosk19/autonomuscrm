using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.Events.EventSourcing;

/// <summary>
/// Interfaz para Event Store (definida en Application para evitar dependencia circular)
/// </summary>
public interface IEventStore
{
    Task SaveEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task<List<IDomainEvent>> GetEventsByTenantAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default);
    Task<List<IDomainEvent>> GetEventsByTypeAsync(string eventType, Guid? tenantId = null, CancellationToken cancellationToken = default);
    Task<List<IDomainEvent>> GetEventsByAggregateIdAsync(Guid aggregateId, int fromVersion = 0, CancellationToken cancellationToken = default);
    Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<int> CountByTenantInRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
    Task<List<string>> GetDistinctEventTypesAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<List<IDomainEvent>> GetEventsByTenantPagedAsync(
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        string? eventType,
        int skip,
        int take,
        CancellationToken cancellationToken = default);
}


