using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Infrastructure.Persistence.EventStore;

/// <summary>
/// Representación de un evento almacenado cuando no hay tipo CLR registrado o falla la deserialización tipada.
/// </summary>
public sealed class PersistedDomainEvent : IDomainEvent
{
    public Guid Id { get; init; }
    public DateTime OccurredOn { get; init; }
    public string EventType { get; init; } = string.Empty;
    public Guid? TenantId { get; init; }
    public Guid? CorrelationId { get; init; }
    public string EventData { get; init; } = string.Empty;

    public static PersistedDomainEvent FromRecord(DomainEventRecord record) => new()
    {
        Id = record.Id,
        OccurredOn = record.OccurredOn,
        EventType = record.EventType,
        TenantId = record.TenantId,
        CorrelationId = record.CorrelationId,
        EventData = record.EventData
    };
}
