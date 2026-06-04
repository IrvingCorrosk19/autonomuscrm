using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.BusinessMemory;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.KnowledgeGraph;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using BusinessMemoryLearning = AutonomusCRM.Application.BusinessMemory.BusinessMemoryLearning;

namespace AutonomusCRM.Infrastructure.Autonomous;

public sealed class AbosOutcomeLearningService : IAbosOutcomeLearningService
{
    private readonly IBusinessMemoryRepository _memory;
    private readonly IUnitOfWork _uow;
    private readonly ApplicationDbContext _db;
    private readonly INbaOutcomeRecordRepository _nbaOutcomes;
    private readonly INextBestActionMlScorer _nbaMl;
    private readonly ISemanticMemoryService _semantic;
    private readonly IKnowledgeGraphRepository _graph;
    private readonly ILogger<AbosOutcomeLearningService> _logger;

    public AbosOutcomeLearningService(
        IBusinessMemoryRepository memory,
        IUnitOfWork uow,
        ApplicationDbContext db,
        INbaOutcomeRecordRepository nbaOutcomes,
        INextBestActionMlScorer nbaMl,
        ISemanticMemoryService semantic,
        IKnowledgeGraphRepository graph,
        ILogger<AbosOutcomeLearningService> logger)
    {
        _memory = memory;
        _uow = uow;
        _db = db;
        _nbaOutcomes = nbaOutcomes;
        _nbaMl = nbaMl;
        _semantic = semantic;
        _graph = graph;
        _logger = logger;
    }

    public async Task<AbosActionRecordedDto> RecordActionExecutedAsync(
        Guid tenantId,
        Guid? executedByUserId,
        string actionType,
        string actionDetail,
        string? insightType,
        string? recommendation,
        string? rationale,
        Guid? customerId,
        Guid? relatedAuditId,
        CancellationToken cancellationToken = default)
    {
        var subjectType = customerId.HasValue ? BusinessMemoryConstants.SubjectCustomer : BusinessMemoryConstants.SubjectTenant;
        var subjectId = customerId ?? tenantId;

        var memory = BusinessMemoryRoot.CreateEpisode(
            tenantId,
            subjectType,
            subjectId,
            $"abos:action:{Guid.NewGuid():N}",
            $"Acción: {actionDetail}",
            BuildSummary(recommendation, rationale, insightType),
            importance: 7,
            sourceChannel: AbosOutcomeLearningConstants.SourceChannel,
            tags: new[]
            {
                AbosOutcomeLearningConstants.TagAction,
                AbosOutcomeLearningConstants.TagPending,
                insightType ?? "general",
                actionType
            });

        await _memory.AddMemoryAsync(memory, cancellationToken);

        var payload = new Dictionary<string, object>
        {
            ["actionType"] = actionType,
            ["actionDetail"] = actionDetail,
            ["insightType"] = insightType ?? "",
            ["recommendation"] = recommendation ?? "",
            ["rationale"] = rationale ?? "",
            ["executedByUserId"] = executedByUserId?.ToString() ?? ""
        };

        await _memory.AddEventAsync(
            BusinessMemoryEvent.FromDomain(
                memory.Id, tenantId, "AbosActionExecuted",
                $"Usuario ejecutó {actionType}: {actionDetail}",
                null, DateTime.UtcNow, payload,
                executedByUserId.HasValue ? "User" : "System", executedByUserId),
            cancellationToken);

        await _memory.AddContextAsync(
            BusinessMemoryContext.Capture(memory.Id, tenantId, "action", payload),
            cancellationToken);

        foreach (var (key, value) in new[]
        {
            ("action.type", actionType),
            ("action.detail", actionDetail),
            ("action.recommendation", recommendation ?? "—"),
            ("action.insight", insightType ?? "—")
        })
        {
            await _memory.AddFactAsync(
                BusinessMemoryFact.Create(memory.Id, tenantId, key, value),
                cancellationToken);
        }

        if (relatedAuditId is Guid auditId)
        {
            var audit = await _db.AiDecisionAudits.AsNoTracking()
                .FirstOrDefaultAsync(a => a.Id == auditId && a.TenantId == tenantId, cancellationToken);
            if (audit != null)
            {
                await _memory.AddDecisionAsync(
                    BusinessMemoryDecision.FromAudit(
                        memory.Id, tenantId, auditId,
                        audit.DecisionType, audit.Action, audit.Reason, audit.DecisionScore,
                        audit.Evidence),
                    cancellationToken);
            }
        }

        if (customerId is Guid cid)
        {
            try
            {
                await _graph.AddEdgeAsync(
                    BusinessKnowledgeGraphEdge.Link(
                        tenantId,
                        BusinessMemoryConstants.SubjectCustomer, cid,
                        "AbosAction", memory.Id,
                        "executed_action"),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Graph edge skipped for action memory {MemoryId}", memory.Id);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);

        try
        {
            await _semantic.StoreMemoryAsync(
                tenantId,
                SemanticMemoryConstants.SourceLearning,
                memory.Id,
                $"abos action {actionType} {actionDetail} recommendation {recommendation}",
                0.85,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Semantic index skipped for action {MemoryId}", memory.Id);
        }

        _logger.LogInformation("ABOS action recorded {ActionType} customer={CustomerId}", actionType, customerId);
        return new AbosActionRecordedDto(memory.Id, memory.EpisodeKey);
    }

    public async Task ResolvePendingActionsForCustomerAsync(
        Guid tenantId,
        Guid customerId,
        bool succeeded,
        string outcomeCategory,
        decimal revenueDelta,
        string narrative,
        CancellationToken cancellationToken = default)
    {
        var pending = await _memory.GetBySubjectAsync(
            tenantId, BusinessMemoryConstants.SubjectCustomer, customerId, 20, cancellationToken);

        var toResolve = new List<BusinessMemoryRoot>();
        foreach (var mem in pending.Where(m => m.Tags.Contains(AbosOutcomeLearningConstants.TagPending))
                     .OrderByDescending(m => m.CreatedAt)
                     .Take(5))
        {
            var existing = await _memory.GetOutcomesForMemoryAsync(mem.Id, cancellationToken);
            if (existing.Count == 0)
                toResolve.Add(mem);
        }

        foreach (var mem in toResolve.Take(3))
        {
            await _memory.AddOutcomeAsync(
                BusinessMemoryOutcome.Record(
                    mem.Id, tenantId, outcomeCategory, succeeded, narrative,
                    revenueDelta, succeeded ? 8 : -5, succeeded ? 3 : -2),
                cancellationToken);

            await _memory.AddFactAsync(
                BusinessMemoryFact.Create(mem.Id, tenantId, "action.resolved", succeeded ? "true" : "false"),
                cancellationToken);

            var facts = await _db.BusinessMemoryFacts.AsNoTracking()
                .Where(f => f.MemoryId == mem.Id && f.TenantId == tenantId)
                .ToListAsync(cancellationToken);

            var actionType = facts.FirstOrDefault(f => f.FactKey == "action.type")?.FactValue ?? "action";
            var recommendation = facts.FirstOrDefault(f => f.FactKey == "action.recommendation")?.FactValue ?? actionType;
            var insight = facts.FirstOrDefault(f => f.FactKey == "action.insight")?.FactValue ?? "general";

            await ApplyLearningAsync(tenantId,
                $"{AbosOutcomeLearningConstants.StrategyPrefixAction}{actionType}",
                actionType, succeeded, narrative, cancellationToken);

            if (!string.IsNullOrWhiteSpace(recommendation) && recommendation != "—")
            {
                await ApplyLearningAsync(tenantId,
                    $"{AbosOutcomeLearningConstants.StrategyPrefixRecommendation}{NormalizeKey(recommendation)}",
                    recommendation, succeeded, narrative, cancellationToken);
            }

            if (insight.Contains("playbook", StringComparison.OrdinalIgnoreCase)
                || actionType.StartsWith("Playbook:", StringComparison.OrdinalIgnoreCase))
            {
                var playbookKey = actionType.StartsWith("Playbook:", StringComparison.OrdinalIgnoreCase)
                    ? actionType["Playbook:".Length..]
                    : insight;
                await ApplyLearningAsync(tenantId,
                    $"{AbosOutcomeLearningConstants.StrategyPrefixPlaybook}{NormalizeKey(playbookKey)}",
                    playbookKey, succeeded, narrative, cancellationToken);
            }

            await _nbaOutcomes.AddAsync(
                NbaOutcomeRecord.FromAction(
                    tenantId, BusinessMemoryConstants.SubjectCustomer, customerId,
                    recommendation, actionType, succeeded, revenueDelta),
                cancellationToken);

            await _nbaMl.RecordOutcomeAsync(
                tenantId, BusinessMemoryConstants.SubjectCustomer, customerId,
                recommendation, outcomeCategory, succeeded, revenueDelta, cancellationToken);

            if (succeeded && revenueDelta > 0)
            {
                await _memory.AddInsightAsync(
                    BusinessMemoryInsight.Create(
                        tenantId, "action_success",
                        $"{actionType} generó {revenueDelta:C}: {narrative}", 0.85,
                        customerId, mem.Id),
                    cancellationToken);
            }
        }

        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<AbosExecutiveLearningDto> GetExecutiveLearningAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var outcomes = await _db.BusinessMemoryOutcomes.AsNoTracking()
            .Where(o => o.TenantId == tenantId)
            .ToListAsync(cancellationToken);

        var learnings = await _memory.GetLearningsAsync(tenantId, 200, cancellationToken);
        var nba = (await _nbaOutcomes.GetRecentAsync(tenantId, 500, cancellationToken)).ToList();
        var audits = await _db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.BusinessSucceeded != null)
            .ToListAsync(cancellationToken);

        var rates = new AbosLearningRatesDto(
            Rate(outcomes.Count, outcomes.Count(o => o.Succeeded)),
            RateByPrefix(learnings, AbosOutcomeLearningConstants.StrategyPrefixPlaybook),
            RateByPrefix(learnings, AbosOutcomeLearningConstants.StrategyPrefixRecommendation),
            Rate(audits.Count, audits.Count(a => a.BusinessSucceeded == true)),
            await SegmentSuccessRateAsync(tenantId, cancellationToken));

        var generated = outcomes.Where(o => o.Succeeded && o.RevenueDelta > 0
            && o.OutcomeCategory is "expansion" or "revenue").Sum(o => o.RevenueDelta);
        var protectedRev = outcomes.Where(o => o.Succeeded && o.RevenueDelta > 0
            && o.OutcomeCategory is "retention" or "renewal").Sum(o => o.RevenueDelta);
        var lost = outcomes.Where(o => !o.Succeeded).Sum(o => Math.Abs(o.RevenueDelta));

        var topActions = learnings
            .Where(l => l.SuccessCount + l.FailureCount >= 1)
            .OrderByDescending(l => l.SuccessRate * (l.SuccessCount + l.FailureCount))
            .Take(8)
            .Select(l => new AbosEffectiveActionDto(
                l.ActionTaken,
                CategoryFromStrategy(l.StrategyKey),
                (decimal)l.SuccessRate,
                l.SuccessCount + l.FailureCount,
                EstimateImpact(l, nba)))
            .ToList();

        if (!topActions.Any() && nba.Any())
        {
            topActions = nba.GroupBy(n => n.RecommendedAction)
                .Select(g => new AbosEffectiveActionDto(
                    g.Key,
                    "recommendation",
                    g.Any() ? (decimal)g.Count(x => x.Converted) * 100m / g.Count() : 0,
                    g.Count(),
                    g.Sum(x => x.ImpactScore)))
                .OrderByDescending(x => x.SuccessRate)
                .Take(8)
                .ToList();
        }

        return new AbosExecutiveLearningDto(rates, generated, protectedRev, lost, topActions);
    }

    public async Task<CustomerActionLearningDto> GetCustomerLearningAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var episodes = await _memory.GetBySubjectAsync(
            tenantId, BusinessMemoryConstants.SubjectCustomer, customerId, 30, cancellationToken);

        var actionEpisodes = episodes
            .Where(e => e.Tags.Contains(AbosOutcomeLearningConstants.TagAction))
            .ToList();

        var worked = new List<AbosActionHistoryItemDto>();
        var failed = new List<AbosActionHistoryItemDto>();

        foreach (var ep in actionEpisodes)
        {
            var outcomes = await _memory.GetOutcomesForMemoryAsync(ep.Id, cancellationToken);
            if (outcomes.Count == 0) continue;

            var facts = await _db.BusinessMemoryFacts.AsNoTracking()
                .Where(f => f.MemoryId == ep.Id)
                .ToListAsync(cancellationToken);

            var action = facts.FirstOrDefault(f => f.FactKey == "action.type")?.FactValue ?? ep.Title;
            var rec = facts.FirstOrDefault(f => f.FactKey == "action.recommendation")?.FactValue ?? "—";

            foreach (var o in outcomes)
            {
                var item = new AbosActionHistoryItemDto(
                    action, rec, o.Narrative, o.RevenueDelta, ep.CreatedAt, o.Succeeded);
                if (o.Succeeded) worked.Add(item);
                else failed.Add(item);
            }
        }

        var learnings = (await _memory.GetLearningsAsync(tenantId, 100, cancellationToken))
            .Where(l => l.ContextPattern.ContainsKey("customerId")
                && l.ContextPattern["customerId"]?.ToString() == customerId.ToString())
            .ToList();

        if (!learnings.Any())
        {
            learnings = (await _memory.GetLearningsAsync(tenantId, 50, cancellationToken))
                .Where(l => l.StrategyKey.StartsWith(AbosOutcomeLearningConstants.StrategyPrefixAction))
                .Take(5)
                .ToList();
        }

        var best = learnings
            .OrderByDescending(l => l.SuccessRate)
            .Take(5)
            .Select(l => new AbosEffectiveActionDto(
                l.ActionTaken, CategoryFromStrategy(l.StrategyKey),
                l.SuccessRate, l.SuccessCount + l.FailureCount, 0))
            .ToList();

        return new CustomerActionLearningDto(worked, failed, best);
    }

    private async Task ApplyLearningAsync(
        Guid tenantId, string strategyKey, string action, bool success, string outcomeLabel,
        CancellationToken cancellationToken)
    {
        var learning = await _memory.GetLearningAsync(tenantId, strategyKey, cancellationToken);
        if (learning is null)
        {
            learning = BusinessMemoryLearning.Start(tenantId, strategyKey, action);
            await _memory.AddLearningAsync(learning, cancellationToken);
        }

        learning.ApplyOutcome(success, outcomeLabel);
        await _memory.UpdateLearningAsync(learning, cancellationToken);
    }

    private async Task<double> SegmentSuccessRateAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var atRisk = await _db.CustomerAnalyticsSnapshots.AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.Segment == IntelligenceConstants.SegmentAtRisk)
            .Select(s => s.CustomerId)
            .Distinct()
            .Take(50)
            .ToListAsync(cancellationToken);

        if (atRisk.Count == 0) return 0;

        var outcomes = await _db.BusinessMemoryOutcomes.AsNoTracking()
            .Where(o => o.TenantId == tenantId)
            .Join(_db.BusinessMemoryRoots.Where(m => m.SubjectType == BusinessMemoryConstants.SubjectCustomer
                && atRisk.Contains(m.SubjectId)),
                o => o.MemoryId, m => m.Id, (o, _) => o)
            .ToListAsync(cancellationToken);

        return Rate(outcomes.Count, outcomes.Count(o => o.Succeeded));
    }

    private static double Rate(int total, int success)
        => total > 0 ? Math.Round(success * 100.0 / total, 1) : 0;

    private static double RateByPrefix(IReadOnlyList<BusinessMemoryLearning> learnings, string prefix)
    {
        var subset = learnings.Where(l => l.StrategyKey.StartsWith(prefix)).ToList();
        if (subset.Count == 0) return 0;
        return Math.Round((double)subset.Average(l => (double)l.SuccessRate), 1);
    }

    private static string CategoryFromStrategy(string key)
    {
        if (key.StartsWith(AbosOutcomeLearningConstants.StrategyPrefixPlaybook)) return "playbook";
        if (key.StartsWith(AbosOutcomeLearningConstants.StrategyPrefixRecommendation)) return "recommendation";
        if (key.StartsWith(AbosOutcomeLearningConstants.StrategyPrefixAgent)) return "agent";
        return "action";
    }

    private static decimal EstimateImpact(BusinessMemoryLearning l, List<NbaOutcomeRecord> nba)
        => nba.Where(n => n.RecommendedAction == l.ActionTaken).Sum(n => n.ImpactScore);

    private static string NormalizeKey(string s)
        => new string(s.ToLowerInvariant().Where(c => char.IsLetterOrDigit(c) || c == '.').Take(48).ToArray());

    private static string BuildSummary(string? recommendation, string? rationale, string? insightType)
    {
        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(recommendation)) parts.Add($"Recomendación: {recommendation}");
        if (!string.IsNullOrWhiteSpace(rationale)) parts.Add($"Motivo: {rationale}");
        if (!string.IsNullOrWhiteSpace(insightType)) parts.Add($"Insight: {insightType}");
        return parts.Count > 0 ? string.Join(" · ", parts) : "Acción ejecutada desde ABOS Action Engine";
    }
}
