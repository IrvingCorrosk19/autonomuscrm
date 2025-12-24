using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Tenants.Events;

public class TenantDeactivatedEvent : DomainEventBase
{
    public override string EventType => "Tenant.Deactivated";

    public TenantDeactivatedEvent(Guid tenantId) : base(tenantId)
    {
    }
}

