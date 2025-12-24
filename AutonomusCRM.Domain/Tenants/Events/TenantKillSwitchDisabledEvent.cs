using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Tenants.Events;

public class TenantKillSwitchDisabledEvent : DomainEventBase
{
    public override string EventType => "Tenant.KillSwitchDisabled";

    public TenantKillSwitchDisabledEvent(Guid tenantId) : base(tenantId)
    {
    }
}

