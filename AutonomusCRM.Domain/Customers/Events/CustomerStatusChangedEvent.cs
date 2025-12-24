using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Customers.Events;

public class CustomerStatusChangedEvent : DomainEventBase
{
    public override string EventType => "Customer.StatusChanged";
    public Guid CustomerId { get; }
    public CustomerStatus OldStatus { get; }
    public CustomerStatus NewStatus { get; }

    public CustomerStatusChangedEvent(Guid customerId, Guid tenantId, CustomerStatus oldStatus, CustomerStatus newStatus) : base(tenantId)
    {
        CustomerId = customerId;
        OldStatus = oldStatus;
        NewStatus = newStatus;
    }
}

