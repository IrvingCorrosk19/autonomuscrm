using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Customers.Events;

public class CustomerLifetimeValueUpdatedEvent : DomainEventBase
{
    public override string EventType => "Customer.LifetimeValueUpdated";
    public Guid CustomerId { get; }
    public decimal LifetimeValue { get; }

    public CustomerLifetimeValueUpdatedEvent(Guid customerId, Guid tenantId, decimal lifetimeValue) : base(tenantId)
    {
        CustomerId = customerId;
        LifetimeValue = lifetimeValue;
    }
}

