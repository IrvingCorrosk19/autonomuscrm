namespace AutonomusCRM.Domain.Events;

/// <summary>
/// Interfaz base para eventos de dominio
/// </summary>
public interface IDomainEvent
{
    Guid Id { get; }
    DateTime OccurredOn { get; }
    string EventType { get; }
    Guid? TenantId { get; }
    Guid? CorrelationId { get; }
}

