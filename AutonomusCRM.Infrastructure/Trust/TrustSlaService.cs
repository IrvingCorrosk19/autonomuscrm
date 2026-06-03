using AutonomusCRM.Application.Trust;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Trust;

public sealed class TrustSlaService : ITrustSlaService
{
    private readonly ApplicationDbContext _db;

    public TrustSlaService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<TrustSlaAlertDto>> GetOverdueApprovalsAsync(
        Guid tenantId, int slaHours = 24, CancellationToken cancellationToken = default)
    {
        var cutoff = DateTime.UtcNow.AddHours(-slaHours);
        var pending = await _db.AiApprovalRequests
            .Where(a => a.TenantId == tenantId && a.Status == "pending" && a.CreatedAt < cutoff)
            .Join(_db.AiDecisionAudits,
                a => a.AuditId,
                d => d.Id,
                (a, d) => new { a.Id, a.AuditId, a.CreatedAt, d.DecisionType })
            .ToListAsync(cancellationToken);

        return pending.Select(p =>
        {
            var hours = (int)(DateTime.UtcNow - p.CreatedAt).TotalHours;
            var severity = hours >= slaHours * 2 ? "critical" : "warning";
            return new TrustSlaAlertDto(p.Id, p.AuditId, p.DecisionType, hours, severity);
        }).ToList();
    }
}
