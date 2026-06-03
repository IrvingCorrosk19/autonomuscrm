using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Trust;

public sealed class TenantTrustPolicyService : ITenantTrustPolicyService
{
    public const string ThresholdKey = "trust.approvalThreshold";
    private const int DefaultThreshold = 70;
    private const int MinThreshold = 50;
    private const int MaxThreshold = 95;

    private readonly ITenantRepository _tenants;
    private readonly IUnitOfWork _uow;

    public TenantTrustPolicyService(ITenantRepository tenants, IUnitOfWork uow)
    {
        _tenants = tenants;
        _uow = uow;
    }

    public async Task<int> GetApprovalThresholdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken);
        if (tenant?.Settings.TryGetValue(ThresholdKey, out var v) == true && int.TryParse(v, out var t))
            return Math.Clamp(t, MinThreshold, MaxThreshold);
        return DefaultThreshold;
    }

    public async Task SetApprovalThresholdAsync(Guid tenantId, int threshold, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found");
        tenant.UpdateSetting(ThresholdKey, Math.Clamp(threshold, MinThreshold, MaxThreshold).ToString());
        await _tenants.UpdateAsync(tenant, cancellationToken);
        await _uow.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> RequiresHumanApprovalAsync(Guid tenantId, int decisionScore, CancellationToken cancellationToken = default)
    {
        var threshold = await GetApprovalThresholdAsync(tenantId, cancellationToken);
        return decisionScore >= threshold;
    }
}

public sealed class TrustMetricsService : ITrustMetricsService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantTrustPolicyService _policy;

    public TrustMetricsService(ApplicationDbContext db, ITenantTrustPolicyService policy)
    {
        _db = db;
        _policy = policy;
    }

    public async Task<TrustMetricsDto> GetMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow.AddDays(-7);
        var pending = await _db.AiApprovalRequests.CountAsync(
            a => a.TenantId == tenantId && a.Status == "pending", cancellationToken);

        var recent = await _db.AiApprovalRequests
            .Where(a => a.TenantId == tenantId && a.CreatedAt >= since)
            .ToListAsync(cancellationToken);

        var pendingScores = await (
            from ap in _db.AiApprovalRequests
            join au in _db.AiDecisionAudits on ap.AuditId equals au.Id
            where ap.TenantId == tenantId && ap.Status == "pending"
            select au.DecisionScore).ToListAsync(cancellationToken);

        var threshold = await _policy.GetApprovalThresholdAsync(tenantId, cancellationToken);

        return new TrustMetricsDto(
            pending,
            recent.Count(r => r.Status == "approved"),
            recent.Count(r => r.Status == "rejected"),
            recent.Count(r => r.Status == "rolled_back"),
            pendingScores.Count > 0 ? pendingScores.Average() : 0,
            threshold);
    }
}
