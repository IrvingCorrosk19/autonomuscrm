using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Domain.Customers.Events;

public class CustomerRiskScoreUpdatedEvent : DomainEventBase
{
    public override string EventType => "Customer.RiskScoreUpdated";
    public Guid CustomerId { get; }
    public int RiskScore { get; }

    public CustomerRiskScoreUpdatedEvent(Guid customerId, Guid tenantId, int riskScore) : base(tenantId)
    {
        CustomerId = customerId;
        RiskScore = riskScore;
    }
}

