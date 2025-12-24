using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Leads.Events;

public class LeadCreatedEvent : DomainEventBase
{
    public override string EventType => "Lead.Created";
    public Guid LeadId { get; }
    public string LeadName { get; }
    public LeadSource Source { get; }

    public LeadCreatedEvent(Guid leadId, Guid tenantId, string leadName, LeadSource source) : base(tenantId)
    {
        LeadId = leadId;
        LeadName = leadName;
        Source = source;
    }
}

