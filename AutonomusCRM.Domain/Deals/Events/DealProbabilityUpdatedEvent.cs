using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealProbabilityUpdatedEvent : DomainEventBase
{
    public override string EventType => "Deal.ProbabilityUpdated";
    public Guid DealId { get; }
    public int? OldProbability { get; }
    public int NewProbability { get; }

    public DealProbabilityUpdatedEvent(Guid dealId, Guid tenantId, int? oldProbability, int newProbability) : base(tenantId)
    {
        DealId = dealId;
        OldProbability = oldProbability;
        NewProbability = newProbability;
    }
}

