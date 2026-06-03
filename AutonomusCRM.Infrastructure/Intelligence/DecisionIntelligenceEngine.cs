using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Intelligence;

public sealed class DecisionIntelligenceEngine : IDecisionIntelligenceEngine
{
    private readonly IGraphReasoningEngine _reasoning;
    private readonly ISemanticMemoryService _semantic;
    private readonly IKnowledgeGraphService _graph;
    private readonly ITenantTrustPolicyService _trustPolicy;
    private readonly IRevenueOsService _revenueOs;
    private readonly ApplicationDbContext _db;

    public DecisionIntelligenceEngine(
        IGraphReasoningEngine reasoning,
        ISemanticMemoryService semantic,
        IKnowledgeGraphService graph,
        ITenantTrustPolicyService trustPolicy,
        IRevenueOsService revenueOs,
        ApplicationDbContext db)
    {
        _reasoning = reasoning;
        _semantic = semantic;
        _graph = graph;
        _trustPolicy = trustPolicy;
        _revenueOs = revenueOs;
        _db = db;
    }

    public async Task<DecisionIntelligenceResultDto> AnalyzeCustomerDecisionAsync(
        Guid tenantId, Guid customerId, string? agentContext, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId, cancellationToken);
        var risk = await _reasoning.ExplainCustomerRiskAsync(tenantId, customerId, cancellationToken);
        var action = await _reasoning.RecommendNextActionAsync(tenantId, customerId, cancellationToken);
        var semantic = await _semantic.GetBusinessContextAsync(tenantId, $"customer {customerId} {agentContext}", cancellationToken);
        var profile = await _semantic.GetOrBuildCustomerProfileAsync(tenantId, customerId, cancellationToken);

        var provisionalScore = risk.Confidence >= 0.75 ? 88 : 72;
        var hitl = await _trustPolicy.RequiresHumanApprovalAsync(tenantId, provisionalScore, cancellationToken);
        var ltv = customer?.LifetimeValue ?? 0m;

        return new DecisionIntelligenceResultDto(
            WhatHappened: $"Customer {customer?.Name ?? customerId.ToString()} assessed by {agentContext ?? "system"}",
            WhyItHappened: risk.Summary,
            Evidence: risk.Evidence.Concat(semantic.SimilarMemories.Select(m => m.Text).Take(3)).ToList(),
            RecommendedAction: action.Summary,
            RiskAssessment: profile.RiskSummary,
            EconomicImpactEstimate: ltv > 0 ? $"LTV ~{ltv:C0}" : "Impact from graph/revenue OS",
            RequiresHumanApproval: hitl,
            ReasoningSummary: semantic.NarrativeSummary,
            SimilarMemories: semantic.SimilarMemories.Select(m => m.Text).Take(5).ToList());
    }

    public async Task<DecisionIntelligenceResultDto> AnalyzeAuditDecisionAsync(
        Guid tenantId, Guid auditId, CancellationToken cancellationToken = default)
    {
        var audit = await _db.AiDecisionAudits.AsNoTracking().FirstOrDefaultAsync(a => a.Id == auditId && a.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Audit not found");

        var explain = await _reasoning.ExplainDecisionAsync(tenantId, auditId, cancellationToken);
        var hitl = await _trustPolicy.RequiresHumanApprovalAsync(tenantId, audit.DecisionScore, cancellationToken);

        return new DecisionIntelligenceResultDto(
            WhatHappened: $"{audit.DecisionType} → {audit.Action} ({audit.Status})",
            WhyItHappened: audit.Reason,
            Evidence: explain.Evidence,
            RecommendedAction: audit.Action,
            RiskAssessment: audit.DecisionScore >= 85 ? "Alto" : audit.DecisionScore >= 70 ? "Medio" : "Bajo",
            EconomicImpactEstimate: audit.BusinessSucceeded == true ? "Positive business outcome recorded" : "Outcome pending or negative",
            RequiresHumanApproval: hitl,
            ReasoningSummary: explain.Summary,
            SimilarMemories: Array.Empty<string>());
    }

    public async Task<DecisionIntelligenceResultDto> ExplainTrustApprovalAsync(
        Guid tenantId, Guid approvalId, CancellationToken cancellationToken = default)
    {
        var approval = await _db.AiApprovalRequests.AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == approvalId && a.TenantId == tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Approval not found");

        var explain = await _reasoning.ExplainDecisionAsync(tenantId, approval.AuditId, cancellationToken);
        return new DecisionIntelligenceResultDto(
            WhatHappened: $"Trust approval {approval.Status} for {approval.DecisionType}",
            WhyItHappened: approval.Explanation,
            Evidence: explain.Evidence,
            RecommendedAction: approval.RecommendedAction,
            RiskAssessment: "HITL policy — autonomous execution blocked until human decision",
            EconomicImpactEstimate: "Prevents unreviewed high-impact autonomous action",
            RequiresHumanApproval: true,
            ReasoningSummary: $"Approval gates execution: {explain.Summary}",
            SimilarMemories: Array.Empty<string>());
    }
}
