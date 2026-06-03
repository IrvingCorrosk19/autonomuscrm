using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class AutonomousRevenueDecisionEngine : IAutonomousRevenueDecisionEngine
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IChurnPredictionV2 _churnPrediction;
    private readonly IExpansionIntelligence _expansionIntel;
    private readonly INpsEngine _npsEngine;
    private readonly ICsatEngine _csatEngine;
    private readonly IBusinessKnowledgeEngine _knowledge;
    private readonly IAutonomousCommunicationsEngine _communications;
    private readonly IAutonomousPlaybookEngine _playbookEngine;
    private readonly IAiDecisionAuditService _audit;
    private readonly Application.Trust.IAiTrustService _trust;
    private readonly Application.Trust.ITenantTrustPolicyService _trustPolicy;
    private readonly ISemanticMemoryService _semanticMemory;

    public AutonomousRevenueDecisionEngine(
        ICustomerRepository customerRepository,
        ICustomerHealthEngine healthEngine,
        IChurnPredictionV2 churnPrediction,
        IExpansionIntelligence expansionIntel,
        INpsEngine npsEngine,
        ICsatEngine csatEngine,
        IBusinessKnowledgeEngine knowledge,
        IAutonomousCommunicationsEngine communications,
        IAutonomousPlaybookEngine playbookEngine,
        IAiDecisionAuditService audit,
        Application.Trust.IAiTrustService trust,
        Application.Trust.ITenantTrustPolicyService trustPolicy,
        ISemanticMemoryService semanticMemory)
    {
        _customerRepository = customerRepository;
        _healthEngine = healthEngine;
        _churnPrediction = churnPrediction;
        _expansionIntel = expansionIntel;
        _npsEngine = npsEngine;
        _csatEngine = csatEngine;
        _knowledge = knowledge;
        _communications = communications;
        _playbookEngine = playbookEngine;
        _audit = audit;
        _trust = trust;
        _trustPolicy = trustPolicy;
        _semanticMemory = semanticMemory;
    }

    public async Task<AutonomousDecisionDto> DecideForCustomerAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
        if (customer == null || customer.TenantId != tenantId)
            throw new InvalidOperationException("Customer not found");

        var health = await _healthEngine.CalculateHealthAsync(tenantId, customerId, cancellationToken);
        var churn = (await _churnPrediction.PredictAsync(tenantId, customerId, cancellationToken)).FirstOrDefault();
        var expansion = (await _expansionIntel.AnalyzeAsync(tenantId, cancellationToken))
            .FirstOrDefault(e => e.CustomerId == customerId);
        var nps = (await _npsEngine.GetSummaryAsync(tenantId, cancellationToken)).ByCustomer
            .FirstOrDefault(n => n.CustomerId == customerId);
        var csat = await _csatEngine.GetSummaryAsync(tenantId, cancellationToken);

        var evidence = new Dictionary<string, object>
        {
            ["HealthScore"] = health.HealthScore,
            ["HealthClass"] = health.Classification,
            ["ChurnProbability"] = churn?.ChurnProbability ?? 0,
            ["NpsScore"] = nps?.LatestScore ?? -1,
            ["CsatAvg"] = csat.AverageScore,
            ["LTV"] = customer.LifetimeValue ?? 0
        };

        var semanticQuery =
            $"customer {customerId} similar clients decisions playbooks campaigns segment health={health.HealthScore} churn={churn?.ChurnProbability ?? 0}";
        var businessContext = await _semanticMemory.GetBusinessContextAsync(tenantId, semanticQuery, cancellationToken);
        evidence["SemanticMemorySummary"] = businessContext.NarrativeSummary;
        if (businessContext.RelatedLearnings.Count > 0)
            evidence["SemanticLearnings"] = string.Join("; ", businessContext.RelatedLearnings.Take(5));

        string decisionType;
        string action;
        int score;

        if (churn?.ChurnProbability >= 75 || health.Classification == CustomerSuccessConstants.HealthCritical)
        {
            decisionType = AutonomousConstants.DecisionRescue;
            action = _knowledge.ResolvePreferredAction(AutonomousConstants.DecisionRescue, tenantId);
            score = 90;
        }
        else if (expansion?.ReadinessLevel == "Ready" && health.HealthScore >= 65)
        {
            decisionType = AutonomousConstants.DecisionExpansion;
            action = "ExecuteExpansionPlaybook";
            score = 85;
        }
        else if (expansion?.OpportunityType == "Upsell" && health.HealthScore >= 70)
        {
            decisionType = AutonomousConstants.DecisionUpsell;
            action = "ProposeUpsell";
            score = 80;
        }
        else if (nps?.Classification == IntelligenceConstants.NpsDetractor || csat.AverageScore < 3)
        {
            decisionType = AutonomousConstants.DecisionEscalation;
            action = "EscalateToExecutive";
            score = 88;
        }
        else if (health.EngagementScore < 35)
        {
            decisionType = AutonomousConstants.DecisionReEngagement;
            action = "ReEngagementCampaign";
            score = 72;
        }
        else if (customer.Metadata.ContainsKey("RenewalInProgress") || evidence["ChurnProbability"] is int cp && cp > 40)
        {
            decisionType = AutonomousConstants.DecisionRenewal;
            action = "ExecuteRenewalPlaybook";
            score = 78;
        }
        else
        {
            decisionType = AutonomousConstants.DecisionNoAction;
            action = "Monitor";
            score = 30;
        }

        if (businessContext.SimilarMemories.Count > 0)
        {
            var top = businessContext.SimilarMemories[0];
            if (top.SourceType == SemanticMemoryConstants.SourceLearning && top.ConfidenceScore >= 0.7)
                score = Math.Min(100, score + 5);
        }

        var reason = $"Autonomous decision: {decisionType} based on health={health.HealthScore}, churn={churn?.ChurnProbability ?? 0}%";
        if (!string.IsNullOrWhiteSpace(businessContext.NarrativeSummary))
            reason += $" | Memory: {businessContext.NarrativeSummary}";
        return new AutonomousDecisionDto(Guid.NewGuid(), decisionType, action, score, reason, evidence, customerId, null);
    }

    public async Task<AutonomousDecisionDto> DecideFromEventAsync(
        IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        if (domainEvent.TenantId == null)
            return new AutonomousDecisionDto(Guid.NewGuid(), AutonomousConstants.DecisionNoAction, "Monitor", 0,
                "No tenant", new Dictionary<string, object>(), null, null);

        var tenantId = domainEvent.TenantId.Value;

        if (domainEvent is CustomerRiskScoreUpdatedEvent rse && rse.RiskScore >= 70)
            return await DecideForCustomerAsync(tenantId, rse.CustomerId, cancellationToken);

        if (domainEvent is CustomerCreatedEvent cce)
            return new AutonomousDecisionDto(Guid.NewGuid(), AutonomousConstants.DecisionRescue,
                "StartOnboarding", 70, "New customer onboarding",
                new Dictionary<string, object>(), cce.CustomerId, null);

        if (domainEvent is DealClosedEvent dcl)
            return new AutonomousDecisionDto(Guid.NewGuid(), AutonomousConstants.DecisionRenewal,
                "PostSaleOnboarding", 75, "Deal closed — retention focus",
                new Dictionary<string, object> { ["dealId"] = dcl.DealId, ["amount"] = dcl.FinalAmount },
                null, dcl.DealId);

        return new AutonomousDecisionDto(Guid.NewGuid(), AutonomousConstants.DecisionNoAction, "Monitor", 20,
            $"Event {domainEvent.EventType}", new Dictionary<string, object>(), null, null);
    }

    public async Task ExecuteDecisionAsync(
        Guid tenantId, AutonomousDecisionDto decision, CancellationToken cancellationToken = default)
    {
        if (decision.DecisionType == AutonomousConstants.DecisionNoAction)
            return;

        var auditId = await _audit.RecordAsync(decision, tenantId, "AutonomousDecisionEngine", cancellationToken);

        if (await _trustPolicy.RequiresHumanApprovalAsync(tenantId, decision.Score, cancellationToken))
        {
            await _trust.QueueForApprovalAsync(
                tenantId, auditId, decision.DecisionType, decision.Action,
                $"Requiere aprobación humana (score={decision.Score}, umbral tenant): {decision.Reason}",
                cancellationToken);
            return;
        }

        try
        {
            if (decision.CustomerId.HasValue)
            {
                var playbook = decision.DecisionType switch
                {
                    AutonomousConstants.DecisionRescue => CustomerSuccessConstants.PlaybookRescue,
                    AutonomousConstants.DecisionRenewal => CustomerSuccessConstants.PlaybookRenewal,
                    AutonomousConstants.DecisionExpansion or AutonomousConstants.DecisionUpsell => CustomerSuccessConstants.PlaybookExpansion,
                    AutonomousConstants.DecisionReEngagement => CustomerSuccessConstants.PlaybookReEngagement,
                    _ => CustomerSuccessConstants.PlaybookAdoption
                };
                await _playbookEngine.StartOrAdvanceAsync(tenantId, decision.CustomerId.Value, playbook, cancellationToken);
            }
            await _communications.ExecuteForDecisionAsync(tenantId, decision, cancellationToken);
            await _audit.MarkExecutionOutcomeAsync(auditId, decision.Action, true, cancellationToken);
            await _knowledge.RecordPatternOutcomeAsync(tenantId, $"{decision.DecisionType}:{decision.Action}", true, cancellationToken);
        }
        catch
        {
            await _audit.MarkExecutionOutcomeAsync(auditId, "Execution failed", false, cancellationToken);
            throw;
        }
    }
}
