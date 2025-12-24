using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealAssignedEvent : DomainEventBase
{
    public override string EventType => "Deal.Assigned";
    public Guid DealId { get; }
    public Guid UserId { get; }

    public DealAssignedEvent(Guid dealId, Guid tenantId, Guid userId) : base(tenantId)
    {
        DealId = dealId;
        UserId = userId;
    }
}

