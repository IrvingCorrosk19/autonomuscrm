using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Leads.Events;

public class LeadScoreUpdatedEvent : DomainEventBase
{
    public override string EventType => "Lead.ScoreUpdated";
    public Guid LeadId { get; }
    public int Score { get; }

    public LeadScoreUpdatedEvent(Guid leadId, Guid tenantId, int score) : base(tenantId)
    {
        LeadId = leadId;
        Score = score;
    }
}

