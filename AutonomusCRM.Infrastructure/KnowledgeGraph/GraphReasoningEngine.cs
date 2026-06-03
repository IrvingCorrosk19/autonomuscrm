using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

public sealed class GraphReasoningEngine : IGraphReasoningEngine
{
    private readonly IKnowledgeGraphService _graph;
    private readonly ISemanticMemoryService _semantic;
    private readonly IBusinessMemoryService _memory;
    private readonly ApplicationDbContext _db;

    public GraphReasoningEngine(
        IKnowledgeGraphService graph,
        ISemanticMemoryService semantic,
        IBusinessMemoryService memory,
        ApplicationDbContext db)
    {
        _graph = graph;
        _semantic = semantic;
        _memory = memory;
        _db = db;
    }

    public async Task<GraphReasoningResultDto> ExplainCustomerRiskAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var kg = await _graph.GetCustomerGraphAsync(tenantId, customerId, cancellationToken);
        var semantic = await _semantic.GetBusinessContextAsync(tenantId, $"customer {customerId} churn risk cancellation", cancellationToken);
        var evidence = kg.RiskSignals.Concat(semantic.RelatedLearnings).Concat(kg.Exploration.Answers.Select(a => a.Answer)).Take(8).ToList();
        var chain = await FindCausalChainAsync(tenantId, customerId, KnowledgeGraphNodeTypes.Customer, cancellationToken);
        var outcomes = await LoadOutcomeCountsAsync(tenantId, customerId, cancellationToken);
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            evidence.Count, kg.Edges.Count, outcomes.Positive, outcomes.Negative,
            RelationshipStrength: kg.Edges.Count > 0 ? Math.Min(1, kg.Edges.Count / 10.0) : 0,
            SemanticMatchScore: semantic.RelatedLearnings.Count > 0 ? 0.7 : 0.2,
            TemporalRelevanceScore: outcomes.LastUtc.HasValue ? 0.8 : 0.3,
            outcomes.LastUtc,
            BasePrior: kg.RiskSignals.Count > 0 ? 0.25 : 0.12));

        return new GraphReasoningResultDto(
            kg.RiskSignals.FirstOrDefault() ?? semantic.NarrativeSummary,
            evidence, chain, confidence, "ABOS-Graph+Semantic");
    }

    public async Task<GraphReasoningResultDto> ExplainCustomerRenewalAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var kg = await _graph.GetCustomerGraphAsync(tenantId, customerId, cancellationToken);
        var answer = kg.Exploration.Answers.FirstOrDefault(a => a.Question.Contains("renovó", StringComparison.OrdinalIgnoreCase));
        var semantic = await _semantic.GetBusinessContextAsync(tenantId, $"customer {customerId} renewal success", cancellationToken);
        var chain = await FindCausalChainAsync(tenantId, customerId, KnowledgeGraphNodeTypes.Customer, cancellationToken);
        var evidence = (answer?.Evidence ?? semantic.RelatedLearnings).ToList();
        var outcomes = await LoadOutcomeCountsAsync(tenantId, customerId, cancellationToken);
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            evidence.Count, kg.Edges.Count, outcomes.Positive, outcomes.Negative,
            kg.RevenueLinks.Count > 0 ? 0.6 : 0.2,
            answer != null ? 0.75 : 0.35,
            0.5, outcomes.LastUtc, 0.18));

        return new GraphReasoningResultDto(
            answer?.Answer ?? semantic.NarrativeSummary,
            evidence, chain, confidence, "ABOS-Graph+Semantic");
    }

    public async Task<GraphReasoningResultDto> ExplainRevenueOutcomeAsync(Guid tenantId, Guid? customerId, CancellationToken cancellationToken = default)
    {
        var rev = await _graph.GetRevenueGraphAsync(tenantId, cancellationToken);
        var q = customerId.HasValue ? $"revenue customer {customerId}" : "revenue tenant outcome";
        var semantic = await _semantic.GetBusinessContextAsync(tenantId, q, cancellationToken);
        var evidence = rev.AttributionChain.Concat(semantic.RelatedLearnings).Take(8).ToList();
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            evidence.Count, rev.AttributionChain.Count, 0, 0,
            rev.AttributionChain.Count > 0 ? 0.5 : 0.1,
            semantic.RelatedLearnings.Count > 0 ? 0.6 : 0.2,
            0.4, null, 0.16));

        return new GraphReasoningResultDto(
            rev.AttributionChain.FirstOrDefault() ?? semantic.NarrativeSummary,
            evidence, rev.AttributionChain, confidence, "ABOS-Revenue-Graph");
    }

    public async Task<GraphReasoningResultDto> RecommendNextActionAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var learnings = await _semantic.GetRelatedLearningsAsync(tenantId, $"customer {customerId} successful action playbook", 5, cancellationToken);
        var similar = await _semantic.FindSimilarMemoriesAsync(tenantId, $"next action customer {customerId}", 5, cancellationToken);
        var action = learnings.FirstOrDefault() ?? similar.FirstOrDefault()?.Text ?? "Monitor — insufficient historical evidence";
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            learnings.Count + similar.Count, similar.Count, similar.Count, 0,
            similar.Count > 0 ? 0.55 : 0.1,
            similar.FirstOrDefault()?.RelevanceScore ?? 0,
            similar.FirstOrDefault()?.CreatedAt != null ? 0.6 : 0.2,
            similar.FirstOrDefault()?.CreatedAt, 0.14));

        return new GraphReasoningResultDto(action, learnings, similar.Select(s => s.Text).ToList(), confidence, "ABOS-Memory");
    }

    public async Task<GraphReasoningResultDto> ExplainDecisionAsync(Guid tenantId, Guid auditId, CancellationToken cancellationToken = default)
    {
        var dg = await _graph.GetDecisionGraphAsync(tenantId, auditId, cancellationToken);
        if (dg is null)
            return new GraphReasoningResultDto("Decision not found", Array.Empty<string>(), Array.Empty<string>(), 0, "ABOS");

        var audit = await _db.AiDecisionAudits.AsNoTracking().FirstOrDefaultAsync(a => a.Id == auditId, cancellationToken);
        var summary = audit is null ? $"{dg.DecisionType}: {dg.Action}" : $"{audit.DecisionType} — {audit.Reason}";
        var evidence = dg.ContextChain.Concat(dg.MemoryLinks).ToList();
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            evidence.Count, dg.OutcomeLinks.Count + dg.RevenueLinks.Count,
            dg.OutcomeLinks.Count, 0,
            dg.RevenueLinks.Count > 0 ? 0.65 : 0.3,
            audit != null ? 0.7 : 0.3,
            audit?.CreatedAt != null ? 0.75 : 0.3,
            audit?.CreatedAt, 0.20));

        return new GraphReasoningResultDto(
            summary, evidence,
            dg.OutcomeLinks.Concat(dg.RevenueLinks).ToList(),
            confidence, "ABOS-Decision-Graph");
    }

    public async Task<IReadOnlyList<string>> FindCausalChainAsync(
        Guid tenantId, Guid fromNodeId, string fromNodeType, CancellationToken cancellationToken = default)
    {
        var edges = await _graph.SearchGraphAsync(tenantId, fromNodeType.Replace("Node", ""), 30, cancellationToken);
        return edges.Edges
            .Where(e => e.SourceId == fromNodeId || e.TargetId == fromNodeId)
            .Select(e => $"{e.SourceType} --{e.Relation}--> {e.TargetType}")
            .Take(12)
            .ToList();
    }

    public async Task<GraphReasoningResultDto> DetectRevenueLeakAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rev = await _graph.GetRevenueGraphAsync(tenantId, cancellationToken);
        var lost = await _db.BusinessMemoryOutcomes.AsNoTracking()
            .Where(o => o.TenantId == tenantId && !o.Succeeded)
            .OrderByDescending(o => o.CreatedAt)
            .Take(10)
            .ToListAsync(cancellationToken);
        var evidence = lost.Select(o => o.Narrative).ToList();
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            evidence.Count, rev.AttributionChain.Count, 0, lost.Count,
            lost.Count > 0 ? 0.4 : 0.1, 0.3, 0.5,
            lost.FirstOrDefault()?.CreatedAt, 0.12));

        return new GraphReasoningResultDto(
            lost.Count > 0 ? $"{lost.Count} negative outcomes in memory" : "No leak pattern in graph yet",
            evidence, rev.AttributionChain, confidence, "ABOS-OutcomeFabric");
    }

    public async Task<GraphReasoningResultDto> DetectExpansionPathAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var kg = await _graph.GetCustomerGraphAsync(tenantId, customerId, cancellationToken);
        var confidence = GraphConfidenceCalculator.Calculate(new GraphConfidenceInput(
            kg.ExpansionSignals.Count + kg.RevenueLinks.Count, kg.Edges.Count,
            kg.ExpansionSignals.Count, 0,
            kg.RevenueLinks.Count > 0 ? 0.7 : 0.2,
            kg.ExpansionSignals.Count > 0 ? 0.65 : 0.2,
            0.45, null, 0.14));

        return new GraphReasoningResultDto(
            kg.ExpansionSignals.FirstOrDefault() ?? "No expansion path detected",
            kg.ExpansionSignals.Concat(kg.RevenueLinks).ToList(),
            kg.Edges.Select(e => e.Relation).Distinct().ToList(),
            confidence, "ABOS-Graph");
    }

    private async Task<(int Positive, int Negative, DateTime? LastUtc)> LoadOutcomeCountsAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken)
    {
        var memoryIds = await _db.BusinessMemoryRoots.AsNoTracking()
            .Where(r => r.TenantId == tenantId && r.SubjectId == customerId)
            .Select(r => r.Id)
            .ToListAsync(cancellationToken);

        if (memoryIds.Count == 0)
            return (0, 0, null);

        var outcomes = await _db.BusinessMemoryOutcomes.AsNoTracking()
            .Where(o => o.TenantId == tenantId && memoryIds.Contains(o.MemoryId))
            .ToListAsync(cancellationToken);
        return (
            outcomes.Count(o => o.Succeeded),
            outcomes.Count(o => !o.Succeeded),
            outcomes.OrderByDescending(o => o.CreatedAt).FirstOrDefault()?.CreatedAt);
    }
}
