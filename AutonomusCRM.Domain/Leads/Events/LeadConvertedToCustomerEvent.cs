using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Leads.Events;

public class LeadConvertedToCustomerEvent : DomainEventBase
{
    public override string EventType => "Lead.ConvertedToCustomer";
    public Guid LeadId { get; }
    public Guid CustomerId { get; }

    public LeadConvertedToCustomerEvent(Guid leadId, Guid tenantId, Guid customerId) : base(tenantId)
    {
        LeadId = leadId;
        CustomerId = customerId;
    }
}

