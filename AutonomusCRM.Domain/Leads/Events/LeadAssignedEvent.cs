using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Leads.Events;

public class LeadAssignedEvent : DomainEventBase
{
    public override string EventType => "Lead.Assigned";
    public Guid LeadId { get; }
    public Guid UserId { get; }

    public LeadAssignedEvent(Guid leadId, Guid tenantId, Guid userId) : base(tenantId)
    {
        LeadId = leadId;
        UserId = userId;
    }
}

