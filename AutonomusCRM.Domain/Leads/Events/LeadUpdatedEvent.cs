using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Leads.Events;

public class LeadUpdatedEvent : DomainEventBase
{
    public override string EventType => "Lead.Updated";
    public Guid LeadId { get; }

    public LeadUpdatedEvent(Guid leadId, Guid tenantId) : base(tenantId)
    {
        LeadId = leadId;
    }
}

