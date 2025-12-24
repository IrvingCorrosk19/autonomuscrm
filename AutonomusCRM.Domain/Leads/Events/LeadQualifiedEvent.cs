using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Leads.Events;

public class LeadQualifiedEvent : DomainEventBase
{
    public override string EventType => "Lead.Qualified";
    public Guid LeadId { get; }

    public LeadQualifiedEvent(Guid leadId, Guid tenantId) : base(tenantId)
    {
        LeadId = leadId;
    }
}

