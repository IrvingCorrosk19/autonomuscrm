using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Deals.Events;

public class DealCreatedEvent : DomainEventBase
{
    public override string EventType => "Deal.Created";
    public Guid DealId { get; }
    public Guid CustomerId { get; }
    public string Title { get; }
    public decimal Amount { get; }

    public DealCreatedEvent(Guid dealId, Guid tenantId, Guid customerId, string title, decimal amount) : base(tenantId)
    {
        DealId = dealId;
        CustomerId = customerId;
        Title = title;
        Amount = amount;
    }
}

