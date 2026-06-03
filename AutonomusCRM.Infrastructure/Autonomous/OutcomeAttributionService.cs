using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Autonomous;

public sealed class OutcomeAttributionService : IOutcomeAttributionService
{
    private readonly ApplicationDbContext _db;
    private readonly IAiDecisionAuditService _audits;
    private readonly INextBestActionMlScorer _nbaMl;
    private readonly IDealRepository _deals;
    private readonly IOutcomeFabricService _outcomeFabric;
    private readonly ILogger<OutcomeAttributionService> _logger;

    public OutcomeAttributionService(
        ApplicationDbContext db,
        IAiDecisionAuditService audits,
        INextBestActionMlScorer nbaMl,
        IDealRepository deals,
        IOutcomeFabricService outcomeFabric,
        ILogger<OutcomeAttributionService> logger)
    {
        _db = db;
        _audits = audits;
        _nbaMl = nbaMl;
        _deals = deals;
        _outcomeFabric = outcomeFabric;
        _logger = logger;
    }

    public Task AttributeDealClosedAsync(DealClosedEvent closedEvent, CancellationToken cancellationToken = default)
        => AttributeForDealAsync(closedEvent.TenantId, closedEvent.DealId, succeeded: true,
            $"Deal closed won amount={closedEvent.FinalAmount:C}",
            closedEvent.FinalAmount, "CloseDeal", "revenue", cancellationToken);

    public Task AttributeDealLostAsync(DealLostEvent lostEvent, CancellationToken cancellationToken = default)
        => AttributeForDealAsync(lostEvent.TenantId, lostEvent.DealId, succeeded: false,
            $"Deal lost: {lostEvent.Reason ?? "no reason"}",
            impact: 0, action: "CloseDeal", category: "revenue", cancellationToken);

    public async Task AttributeRenewalAsync(Guid tenantId, Guid customerId, bool renewed, string detail, CancellationToken cancellationToken = default)
    {
        await AttributeForCustomerAsync(tenantId, customerId, renewed, detail, renewed ? 1m : 0m,
            "RenewContract", "retention", cancellationToken);
    }

    public async Task AttributeChurnAsync(Guid tenantId, Guid customerId, string detail, CancellationToken cancellationToken = default)
    {
        await AttributeForCustomerAsync(tenantId, customerId, succeeded: false, detail, 0m,
            "PreventChurn", "retention", cancellationToken);
    }

    public async Task AttributeExpansionAsync(Guid tenantId, Guid customerId, decimal amount, string detail, CancellationToken cancellationToken = default)
    {
        await AttributeForCustomerAsync(tenantId, customerId, succeeded: true, detail, amount,
            "Upsell", "expansion", cancellationToken);
    }

    public async Task AttributePaymentAsync(Guid tenantId, Guid customerId, decimal amount, bool succeeded, string detail, CancellationToken cancellationToken = default)
    {
        await AttributeForCustomerAsync(tenantId, customerId, succeeded, detail, amount,
            "CollectPayment", "billing", cancellationToken);
    }

    private async Task AttributeForDealAsync(
        Guid? tenantId, Guid dealId, bool succeeded, string detail, decimal impact,
        string action, string category, CancellationToken cancellationToken)
    {
        if (tenantId == null) return;
        var deal = await _deals.GetByIdAsync(dealId, cancellationToken);
        if (deal == null) return;

        var audits = await PendingAuditsForDealAsync(tenantId.Value, dealId, deal.CustomerId, cancellationToken);
        foreach (var audit in audits)
        {
            await _audits.MarkBusinessOutcomeAsync(audit.Id, succeeded, detail, cancellationToken);
            await _outcomeFabric.RecordBusinessOutcomeAsync(audit.Id, succeeded, detail, impact, cancellationToken);
        }

        if (deal.CustomerId != Guid.Empty)
        {
            await _nbaMl.RecordOutcomeAsync(
                tenantId.Value, "Customer", deal.CustomerId, action, category,
                converted: succeeded, impact: impact, cancellationToken);
        }

        _logger.LogInformation("Outcome {Success} deal {DealId} audits={Count}", succeeded, dealId, audits.Count);
    }

    private async Task AttributeForCustomerAsync(
        Guid tenantId, Guid customerId, bool succeeded, string detail, decimal impact,
        string action, string category, CancellationToken cancellationToken)
    {
        var audits = await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId && a.CustomerId == customerId && a.BusinessSucceeded == null)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        foreach (var audit in audits)
        {
            await _audits.MarkBusinessOutcomeAsync(audit.Id, succeeded, detail, cancellationToken);
            await _outcomeFabric.RecordBusinessOutcomeAsync(audit.Id, succeeded, detail, impact, cancellationToken);
        }

        await _nbaMl.RecordOutcomeAsync(tenantId, "Customer", customerId, action, category,
            converted: succeeded, impact: impact, cancellationToken);

        _logger.LogInformation("Outcome {Success} customer {CustomerId} audits={Count}", succeeded, customerId, audits.Count);
    }

    private async Task<List<AiDecisionAudit>> PendingAuditsForDealAsync(
        Guid tenantId, Guid dealId, Guid customerId, CancellationToken cancellationToken)
    {
        var audits = await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId && a.DealId == dealId && a.BusinessSucceeded == null)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync(cancellationToken);

        if (audits.Count == 0 && customerId != Guid.Empty)
        {
            audits = await _db.AiDecisionAudits
                .Where(a => a.TenantId == tenantId && a.CustomerId == customerId && a.BusinessSucceeded == null)
                .OrderByDescending(a => a.CreatedAt)
                .Take(3)
                .ToListAsync(cancellationToken);
        }

        return audits;
    }
}
