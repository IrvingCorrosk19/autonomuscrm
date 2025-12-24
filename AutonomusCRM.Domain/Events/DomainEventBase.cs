namespace AutonomusCRM.Domain.Events;

/// <summary>
/// Clase base para eventos de dominio
/// </summary>
public abstract class DomainEventBase : IDomainEvent
{
    public Guid Id { get; }
    public DateTime OccurredOn { get; }
    public abstract string EventType { get; }
    public Guid? TenantId { get; protected set; }
    public Guid? CorrelationId { get; protected set; }

    protected DomainEventBase()
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        CorrelationId = Guid.NewGuid(); // Generar CorrelationId por defecto
    }

    protected DomainEventBase(Guid? tenantId, Guid? correlationId = null)
    {
        Id = Guid.NewGuid();
        OccurredOn = DateTime.UtcNow;
        TenantId = tenantId;
        CorrelationId = correlationId ?? Guid.NewGuid(); // Siempre tener CorrelationId
    }
}

