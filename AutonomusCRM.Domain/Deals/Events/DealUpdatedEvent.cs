using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealUpdatedEvent : DomainEventBase
{
    public override string EventType => "Deal.Updated";
    public Guid DealId { get; }

    public DealUpdatedEvent(Guid dealId, Guid tenantId) : base(tenantId)
    {
        DealId = dealId;
    }
}

