using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Leads.Events;

public class LeadStatusChangedEvent : DomainEventBase
{
    public override string EventType => "Lead.StatusChanged";
    public Guid LeadId { get; }
    public LeadStatus OldStatus { get; }
    public LeadStatus NewStatus { get; }

    public LeadStatusChangedEvent(Guid leadId, Guid tenantId, LeadStatus oldStatus, LeadStatus newStatus) : base(tenantId)
    {
        LeadId = leadId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}

