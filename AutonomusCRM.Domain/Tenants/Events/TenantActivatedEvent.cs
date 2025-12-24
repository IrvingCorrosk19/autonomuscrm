using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Tenants.Events;

public class TenantActivatedEvent : DomainEventBase
{
    public override string EventType => "Tenant.Activated";

    public TenantActivatedEvent(Guid tenantId) : base(tenantId)
    {
    }
}

