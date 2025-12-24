using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Customers.Events;

public class CustomerUpdatedEvent : DomainEventBase
{
    public override string EventType => "Customer.Updated";
    public Guid CustomerId { get; }

    public CustomerUpdatedEvent(Guid customerId, Guid tenantId) : base(tenantId)
    {
        CustomerId = customerId;
    }
}

