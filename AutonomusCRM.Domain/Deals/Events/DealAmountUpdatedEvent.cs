using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealAmountUpdatedEvent : DomainEventBase
{
    public override string EventType => "Deal.AmountUpdated";
    public Guid DealId { get; }
    public decimal Amount { get; }

    public DealAmountUpdatedEvent(Guid dealId, Guid tenantId, decimal amount) : base(tenantId)
    {
        DealId = dealId;
        Amount = amount;
    }
}

