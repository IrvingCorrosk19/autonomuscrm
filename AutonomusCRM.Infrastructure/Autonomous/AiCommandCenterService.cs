using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Autonomous;

public sealed class AiCommandCenterService : IAiCommandCenterService
{
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
        var since24h = DateTime.UtcNow.AddHours(-24);
        var since7d = DateTime.UtcNow.AddDays(-7);

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

        var revenueSum = revenue7d
            .Select(e => e.TryGetValue("outcomeFabric.revenueImpact", out var v) ? Convert.ToDecimal(v) : 0m)
            .Sum();

        var incomplete = (await _outcomeFabric.GetIncompleteAsync(tenantId, 200, cancellationToken)).Count;

        var recent = await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(15)
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

}
