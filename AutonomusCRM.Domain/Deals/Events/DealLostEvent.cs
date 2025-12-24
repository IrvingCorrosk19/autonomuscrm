using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealLostEvent : DomainEventBase
{
    public override string EventType => "Deal.Lost";
    public Guid DealId { get; }
    public string? Reason { get; }

    public DealLostEvent(Guid dealId, Guid tenantId, string? reason) : base(tenantId)
    {
        DealId = dealId;
        Reason = reason;
    }
}

