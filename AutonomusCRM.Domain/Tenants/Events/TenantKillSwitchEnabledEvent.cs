using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Tenants.Events;

public class TenantKillSwitchEnabledEvent : DomainEventBase
{
    public override string EventType => "Tenant.KillSwitchEnabled";

    public TenantKillSwitchEnabledEvent(Guid tenantId) : base(tenantId)
    {
    }
}

