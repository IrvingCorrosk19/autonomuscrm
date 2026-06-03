using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Autonomous;

public sealed class AiCommandCenterService : IAiCommandCenterService
{
    private const string RevenueKey = "outcomeFabric.revenueImpact";

    private static readonly (string Key, string Display, string[] Patterns)[] WorkforceRoster =
    {
        ("sales", "Sales Agent", new[] { "Sales", "Deal", "Lead", "DealStrategy", "LeadIntelligence" }),
        ("renewal", "Renewal Agent", new[] { "Renewal" }),
        ("churn", "Churn Agent", new[] { "Churn", "Risk", "CustomerRisk", "Rescue" }),
        ("expansion", "Expansion Agent", new[] { "Expansion", "Upsell" }),
        ("customer", "Customer Agent", new[] { "Customer", "Communication" }),
        ("operations", "Operations Agent", new[] { "Automation", "Compliance", "DataQuality", "Operations" })
    };

    private readonly ApplicationDbContext _db;
    private readonly INextBestActionEngine _nba;
    private readonly ITenantTrustPolicyService _trustPolicy;
    private readonly IOutcomeFabricService _outcomeFabric;

    public AiCommandCenterService(
        ApplicationDbContext db,
        INextBestActionEngine nba,
        ITenantTrustPolicyService trustPolicy,
        IOutcomeFabricService outcomeFabric)
    {
        _db = db;
        _nba = nba;
        _trustPolicy = trustPolicy;
        _outcomeFabric = outcomeFabric;
    }

    public async Task<AiCommandCenterDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var flow = await GetFlowCommandAsync(tenantId, 7, cancellationToken);
        return flow.Dashboard;
    }

    public async Task<FlowCommandViewDto> GetFlowCommandAsync(
        Guid tenantId, int periodDays = 7, CancellationToken cancellationToken = default)
    {
        periodDays = Math.Clamp(periodDays, 1, 90);
        var sincePeriod = DateTime.UtcNow.AddDays(-periodDays);
        var since24h = DateTime.UtcNow.AddHours(-24);
        var since7d = DateTime.UtcNow.AddDays(-7);

        var dashboard = await BuildDashboardCoreAsync(tenantId, since24h, since7d, cancellationToken);

        var auditsPeriod = await _db.AiDecisionAudits
            .AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= sincePeriod)
            .ToListAsync(cancellationToken);

        var (generated, protectedRev) = SplitRevenueImpact(auditsPeriod);
        var hasData = auditsPeriod.Count > 0
            || dashboard.PendingApprovals > 0
            || dashboard.AtRiskCustomers.Count > 0
            || dashboard.RecentDecisions.Count > 0;

        var revenueSpark = BuildDailySparkline(auditsPeriod, sincePeriod, a => GetRevenueFromEvidence(a.Evidence) ?? 0m);
        var decisionsSpark = BuildDailySparkline(auditsPeriod, sincePeriod, _ => 1m);
        var renewalSpark = BuildDailySparkline(
            auditsPeriod.Where(a => a.DecisionType.Contains("renew", StringComparison.OrdinalIgnoreCase)).ToList(),
            sincePeriod, _ => 1m);
        var expansionSpark = BuildDailySparkline(
            auditsPeriod.Where(a => a.DecisionType.Contains("expansion", StringComparison.OrdinalIgnoreCase)
                || a.DecisionType.Contains("upsell", StringComparison.OrdinalIgnoreCase)).ToList(),
            sincePeriod, _ => 1m);
        var churnSpark = BuildDailySparkline(
            auditsPeriod.Where(a => a.DecisionType.Contains("churn", StringComparison.OrdinalIgnoreCase)
                || a.DecisionType.Contains("rescue", StringComparison.OrdinalIgnoreCase)).ToList(),
            sincePeriod, _ => 1m);

        var workforce = BuildWorkforce(auditsPeriod, since24h, since7d);
        var pipeline = await GetPipelineSnapshotAsync(tenantId, cancellationToken);
        var incomplete = await _outcomeFabric.GetIncompleteAsync(tenantId, 8, cancellationToken);
        var incompleteDtos = new List<OutcomeFabricStatusDto>();
        foreach (var audit in incomplete)
        {
            var status = await _outcomeFabric.GetStatusAsync(audit.Id, cancellationToken);
            if (status != null) incompleteDtos.Add(status);
        }

        hasData = hasData || pipeline.Any(p => p.DealCount > 0);

        return new FlowCommandViewDto(
            dashboard,
            generated,
            protectedRev,
            generated + protectedRev,
            hasData,
            periodDays,
            revenueSpark,
            renewalSpark,
            expansionSpark,
            churnSpark,
            workforce,
            pipeline,
            incompleteDtos);
    }

    public async Task<IReadOnlyList<DecisionHistoryRow>> GetDecisionHistoryAsync(
        Guid tenantId,
        string? status = null,
        string? agent = null,
        int? minScore = null,
        int take = 100,
        CancellationToken cancellationToken = default)
    {
        var q = _db.AiDecisionAudits.AsNoTracking().Where(a => a.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status))
            q = q.Where(a => a.Status == status);
        if (!string.IsNullOrWhiteSpace(agent))
            q = q.Where(a => a.AgentName != null && a.AgentName.Contains(agent));
        if (minScore.HasValue)
            q = q.Where(a => a.DecisionScore >= minScore.Value);

        var rows = await q.OrderByDescending(a => a.CreatedAt).Take(take).ToListAsync(cancellationToken);
        var customerIds = rows.Where(r => r.CustomerId.HasValue).Select(r => r.CustomerId!.Value).Distinct().ToList();
        var customers = await _db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return rows.Select(a => new DecisionHistoryRow(
            a.Id,
            a.AgentName,
            a.DecisionType,
            a.Action,
            a.DecisionScore,
            a.Status,
            a.CreatedAt,
            a.BusinessSucceeded,
            GetRevenueFromEvidence(a.Evidence),
            a.CustomerId is Guid cid && customers.TryGetValue(cid, out var name) ? name : null)).ToList();
    }

    public async Task<FlowOutcomesSummaryDto> GetOutcomesSummaryAsync(
        Guid tenantId, int periodDays = 30, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-Math.Clamp(periodDays, 1, 365));
        var audits = await _db.AiDecisionAudits.AsNoTracking()
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= since)
            .ToListAsync(cancellationToken);

        var (generated, protectedRev) = SplitRevenueImpact(audits);
        var incomplete = (await _outcomeFabric.GetIncompleteAsync(tenantId, 100, cancellationToken)).Count;
        var complete = audits.Count(a => a.BusinessRecordedAt.HasValue);

        var recentChains = new List<OutcomeFabricStatusDto>();
        foreach (var audit in audits.OrderByDescending(a => a.CreatedAt).Take(20))
        {
            var s = await _outcomeFabric.GetStatusAsync(audit.Id, cancellationToken);
            if (s != null) recentChains.Add(s);
        }

        return new FlowOutcomesSummaryDto(generated, protectedRev, complete, incomplete, recentChains);
    }

    public async Task<IReadOnlyList<PlaybookStateRow>> GetPlaybooksAsync(
        Guid tenantId, int take = 50, CancellationToken cancellationToken = default)
    {
        var states = await _db.AutonomousPlaybookStates.AsNoTracking()
            .Where(p => p.TenantId == tenantId)
            .OrderByDescending(p => p.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

        var customerIds = states.Select(s => s.CustomerId).Distinct().ToList();
        var names = await _db.Customers.AsNoTracking()
            .Where(c => customerIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, c => c.Name, cancellationToken);

        return states.Select(s => new PlaybookStateRow(
            s.Id,
            s.CustomerId,
            names.TryGetValue(s.CustomerId, out var n) ? n : null,
            s.PlaybookType,
            s.Status,
            s.CurrentStepIndex,
            s.TotalSteps,
            s.NextActionAt)).ToList();
    }

    private async Task<AiCommandCenterDto> BuildDashboardCoreAsync(
        Guid tenantId, DateTime since24h, DateTime since7d, CancellationToken cancellationToken)
    {
        var pending = await _db.AiApprovalRequests.CountAsync(
            a => a.TenantId == tenantId && a.Status == "pending", cancellationToken);

        var decisions24h = await _db.AiDecisionAudits.CountAsync(
            a => a.TenantId == tenantId && a.CreatedAt >= since24h, cancellationToken);

        var outcomes7d = await _db.AiDecisionAudits.CountAsync(
            a => a.TenantId == tenantId && a.BusinessRecordedAt >= since7d, cancellationToken);

        var revenue7d = await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId && a.BusinessRecordedAt >= since7d && a.BusinessSucceeded == true)
            .Select(a => a.Evidence)
            .ToListAsync(cancellationToken);

        var revenueSum = revenue7d.Select(e => GetRevenueFromEvidence(e) ?? 0m).Sum();
        var incomplete = (await _outcomeFabric.GetIncompleteAsync(tenantId, 200, cancellationToken)).Count;

        var recent = await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(20)
            .Select(a => new CommandCenterDecisionRow(
                a.Id, a.DecisionType, a.Action, a.DecisionScore, a.Status, a.CreatedAt, a.BusinessSucceeded))
            .ToListAsync(cancellationToken);

        var nba = await _nba.GetForTenantAsync(tenantId, cancellationToken);
        var topNba = nba.Take(8).Select(n => new CommandCenterNbaRow(n.EntityName, n.RecommendedAction, n.PriorityScore, n.Rationale)).ToList();

        var atRisk = nba.Where(n => n.RecommendedAction.Contains("churn", StringComparison.OrdinalIgnoreCase)
            || n.RecommendedAction.Contains("rescue", StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .Select(n => new CommandCenterCustomerRow(n.EntityId, n.EntityName, n.Rationale, n.PriorityScore))
            .ToList();

        var expansion = nba.Where(n => n.RecommendedAction.Contains("upsell", StringComparison.OrdinalIgnoreCase)
            || n.RecommendedAction.Contains("expansion", StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .Select(n => new CommandCenterCustomerRow(n.EntityId, n.EntityName, n.Rationale, n.PriorityScore))
            .ToList();

        var renewal = nba.Where(n => n.RecommendedAction.Contains("renew", StringComparison.OrdinalIgnoreCase))
            .Take(6)
            .Select(n => new CommandCenterCustomerRow(n.EntityId, n.EntityName, n.Rationale, n.PriorityScore))
            .ToList();

        var agentRows = await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId && a.AgentName != null && a.CreatedAt >= since7d)
            .GroupBy(a => a.AgentName!)
            .Select(g => new { Name = g.Key, Actions24h = g.Count(a => a.CreatedAt >= since24h), Outcomes = g.Count(a => a.BusinessRecordedAt >= since7d) })
            .Take(8)
            .ToListAsync(cancellationToken);

        var activeAgents = agentRows
            .Select(a => new CommandCenterAgentRow(a.Name, a.Actions24h, a.Outcomes, 0m))
            .ToList();

        var threshold = await _trustPolicy.GetApprovalThresholdAsync(tenantId, cancellationToken);

        return new AiCommandCenterDto(
            pending, decisions24h, outcomes7d, threshold, revenueSum, incomplete,
            atRisk, expansion, renewal, activeAgents, recent, topNba);
    }

    private async Task<IReadOnlyList<PipelineStageSnapshot>> GetPipelineSnapshotAsync(
        Guid tenantId, CancellationToken cancellationToken)
    {
        var open = await _db.Deals.AsNoTracking()
            .Where(d => d.TenantId == tenantId && d.Status == DealStatus.Open)
            .GroupBy(d => d.Stage)
            .Select(g => new PipelineStageSnapshot(g.Key.ToString(), g.Count(), g.Sum(d => d.Amount)))
            .ToListAsync(cancellationToken);
        return open;
    }

    private static IReadOnlyList<WorkforceAgentDto> BuildWorkforce(
        List<AiDecisionAudit> auditsPeriod, DateTime since24h, DateTime since7d)
    {
        var list = new List<WorkforceAgentDto>();
        foreach (var (key, display, patterns) in WorkforceRoster)
        {
            var matched = auditsPeriod.Where(a =>
                patterns.Any(p => (a.AgentName ?? a.DecisionType).Contains(p, StringComparison.OrdinalIgnoreCase))).ToList();
            var actions24 = matched.Count(a => a.CreatedAt >= since24h);
            var actions7 = matched.Count;
            var outcomes = matched.Count(a => a.BusinessRecordedAt >= since7d);
            var rev = matched.Where(a => a.BusinessRecordedAt >= since7d)
                .Sum(a => GetRevenueFromEvidence(a.Evidence) ?? 0m);
            var status = actions24 > 0 ? "active" : actions7 > 0 ? "idle" : "standby";
            list.Add(new WorkforceAgentDto(key, display, actions24, actions7, outcomes, rev, status));
        }
        return list;
    }

    private static (decimal Generated, decimal Protected) SplitRevenueImpact(List<AiDecisionAudit> audits)
    {
        decimal gen = 0, prot = 0;
        foreach (var a in audits.Where(a => a.BusinessSucceeded == true))
        {
            var rev = GetRevenueFromEvidence(a.Evidence) ?? 0m;
            if (rev == 0) continue;
            if (IsProtectionDecision(a.DecisionType))
                prot += rev;
            else
                gen += rev;
        }
        return (gen, prot);
    }

    private static bool IsProtectionDecision(string decisionType) =>
        decisionType.Contains("churn", StringComparison.OrdinalIgnoreCase)
        || decisionType.Contains("rescue", StringComparison.OrdinalIgnoreCase)
        || decisionType.Contains("retention", StringComparison.OrdinalIgnoreCase);

    private static decimal? GetRevenueFromEvidence(Dictionary<string, object> evidence)
    {
        if (!evidence.TryGetValue(RevenueKey, out var v)) return null;
        return v switch
        {
            decimal d => d,
            double dbl => (decimal)dbl,
            int i => i,
            long l => l,
            _ => null
        };
    }

    private static IReadOnlyList<FlowSparklinePoint> BuildDailySparkline(
        List<AiDecisionAudit> audits, DateTime since, Func<AiDecisionAudit, decimal> valueSelector)
    {
        var days = (int)Math.Ceiling((DateTime.UtcNow - since).TotalDays);
        days = Math.Clamp(days, 7, 14);
        var points = new List<FlowSparklinePoint>();
        for (var i = days - 1; i >= 0; i--)
        {
            var day = DateTime.UtcNow.Date.AddDays(-i);
            var next = day.AddDays(1);
            var dayAudits = audits.Where(a => a.CreatedAt >= day && a.CreatedAt < next).ToList();
            var value = dayAudits.Sum(valueSelector);
            points.Add(new FlowSparklinePoint(day.ToString("dd MMM"), value));
        }
        return points;
    }
}
