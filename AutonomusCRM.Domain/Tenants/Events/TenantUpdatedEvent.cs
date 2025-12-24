using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Tenants.Events;

public class TenantUpdatedEvent : DomainEventBase
{
    public override string EventType => "Tenant.Updated";
    public string TenantName { get; }

    public TenantUpdatedEvent(Guid tenantId, string tenantName) : base(tenantId)
    {
        TenantName = tenantName;
    }
}

