using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Customers.Events;

public class CustomerCreatedEvent : DomainEventBase
{
    public override string EventType => "Customer.Created";
    public Guid CustomerId { get; }
    public string CustomerName { get; }

    public CustomerCreatedEvent(Guid customerId, Guid tenantId, string customerName) : base(tenantId)
    {
        CustomerId = customerId;
        CustomerName = customerName;
    }
}

