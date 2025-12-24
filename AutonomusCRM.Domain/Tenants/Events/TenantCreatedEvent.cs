using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Tenants.Events;

public class TenantCreatedEvent : DomainEventBase
{
    public override string EventType => "Tenant.Created";
    public string TenantName { get; }

    public TenantCreatedEvent(Guid tenantId, string tenantName) : base(tenantId)
    {
        TenantName = tenantName;
    }
}

