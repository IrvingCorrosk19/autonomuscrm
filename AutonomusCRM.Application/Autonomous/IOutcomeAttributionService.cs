using AutonomusCRM.Domain.Deals.Events;

namespace AutonomusCRM.Application.Autonomous;

/// <summary>Links real business outcomes (won/lost/renewal/churn/expansion/payment) to AI audits and NBA ML.</summary>
public interface IOutcomeAttributionService
{
    Task AttributeDealClosedAsync(DealClosedEvent closedEvent, CancellationToken cancellationToken = default);
    Task AttributeDealLostAsync(DealLostEvent lostEvent, CancellationToken cancellationToken = default);
    Task AttributeRenewalAsync(Guid tenantId, Guid customerId, bool renewed, string detail, CancellationToken cancellationToken = default);
    Task AttributeChurnAsync(Guid tenantId, Guid customerId, string detail, CancellationToken cancellationToken = default);
    Task AttributeExpansionAsync(Guid tenantId, Guid customerId, decimal amount, string detail, CancellationToken cancellationToken = default);
    Task AttributePaymentAsync(Guid tenantId, Guid customerId, decimal amount, bool succeeded, string detail, CancellationToken cancellationToken = default);
}
