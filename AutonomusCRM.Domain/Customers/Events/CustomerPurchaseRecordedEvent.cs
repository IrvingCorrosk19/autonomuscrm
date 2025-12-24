using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Customers.Events;

public class CustomerPurchaseRecordedEvent : DomainEventBase
{
    public override string EventType => "Customer.PurchaseRecorded";
    public Guid CustomerId { get; }
    public DateTime PurchaseDate { get; }

    public CustomerPurchaseRecordedEvent(Guid customerId, Guid tenantId, DateTime purchaseDate) : base(tenantId)
    {
        CustomerId = customerId;
        PurchaseDate = purchaseDate;
    }
}

