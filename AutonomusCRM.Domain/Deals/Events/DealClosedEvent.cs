using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealClosedEvent : DomainEventBase
{
    public override string EventType => "Deal.Closed";
    public Guid DealId { get; }
    public DateTime ClosedAt { get; }
    public decimal FinalAmount { get; }

    public DealClosedEvent(Guid dealId, Guid tenantId, DateTime closedAt, decimal finalAmount) : base(tenantId)
    {
        DealId = dealId;
        ClosedAt = closedAt;
        FinalAmount = finalAmount;
    }
}

