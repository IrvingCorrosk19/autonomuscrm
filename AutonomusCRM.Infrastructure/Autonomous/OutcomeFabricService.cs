using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Autonomous;

public sealed class OutcomeFabricService : IOutcomeFabricService
{
    private const string KeyRevenue = "outcomeFabric.revenueImpact";
    private const string KeyLearning = "outcomeFabric.learningStatus";
    private const string KeyExpectedRisk = "outcomeFabric.expectedRisk";
    private const string KeyExpectedImpact = "outcomeFabric.expectedImpact";

    private readonly ApplicationDbContext _db;
    private readonly IAiDecisionAuditRepository _audits;
    private readonly IUnitOfWork _uow;

    public OutcomeFabricService(ApplicationDbContext db, IAiDecisionAuditRepository audits, IUnitOfWork uow)
    {
        _db = db;
        _audits = audits;
        _uow = uow;
    }

    public async Task RecordExecutionAsync(
        Guid auditId, bool success, string detail, decimal? revenueImpact = null, CancellationToken cancellationToken = default)
    {
        var audit = await _audits.GetByIdAsync(auditId, cancellationToken);
        if (audit == null) return;
        if (success) audit.MarkExecuted(detail);
        else audit.MarkFailed(detail);
        if (revenueImpact.HasValue)
            audit.Evidence[KeyRevenue] = revenueImpact.Value;
        audit.Evidence[KeyLearning] = success ? "execution_recorded" : "execution_failed";
        await _audits.UpdateAsync(audit, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task RecordBusinessOutcomeAsync(
        Guid auditId, bool succeeded, string detail, decimal revenueImpact, CancellationToken cancellationToken = default)
    {
        var audit = await _audits.GetByIdAsync(auditId, cancellationToken);
        if (audit == null) return;
        audit.MarkBusinessOutcome(succeeded, detail);
        audit.Evidence[KeyRevenue] = revenueImpact;
        audit.Evidence[KeyLearning] = succeeded ? "outcome_complete" : "outcome_negative";
        await _audits.UpdateAsync(audit, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<OutcomeFabricStatusDto?> GetStatusAsync(Guid auditId, CancellationToken cancellationToken = default)
    {
        var audit = await _audits.GetByIdAsync(auditId, cancellationToken);
        if (audit == null) return null;
        var hasExec = audit.Status is AutonomousConstants.AuditExecuted or AutonomousConstants.AuditFailed;
        decimal? rev = audit.Evidence.TryGetValue(KeyRevenue, out var r) && r is decimal d ? d
            : audit.Evidence.TryGetValue(KeyRevenue, out var r2) && r2 is double dbl ? (decimal)dbl : null;
        var learning = audit.Evidence.TryGetValue(KeyLearning, out var l) ? l?.ToString() ?? "pending" : "pending";
        return new OutcomeFabricStatusDto(
            audit.Id, true, hasExec, audit.BusinessRecordedAt.HasValue, rev, learning);
    }

    public async Task<IReadOnlyList<AiDecisionAudit>> GetIncompleteAsync(
        Guid tenantId, int take = 50, CancellationToken cancellationToken = default)
    {
        return await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId
                && (a.BusinessRecordedAt == null || a.Status == AutonomousConstants.AuditPending))
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    public static void EnrichDecisionEvidence(Dictionary<string, object> evidence, decimal expectedImpact, string expectedRisk)
    {
        evidence[KeyExpectedImpact] = expectedImpact;
        evidence[KeyExpectedRisk] = expectedRisk;
        evidence[KeyLearning] = "decision_created";
    }
}
