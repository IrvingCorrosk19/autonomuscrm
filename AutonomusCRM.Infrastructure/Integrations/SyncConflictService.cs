using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class SyncConflictService : ISyncConflictService
{
    private readonly ApplicationDbContext _db;
    private readonly ITenantIntegrationRepository _integrations;

    public SyncConflictService(ApplicationDbContext db, ITenantIntegrationRepository integrations)
    {
        _db = db;
        _integrations = integrations;
    }

    public async Task<IReadOnlyList<SyncConflictDto>> DetectConflictsAsync(
        Guid tenantId, string provider, CancellationToken cancellationToken = default)
    {
        var conn = await _integrations.GetAsync(tenantId, provider, cancellationToken);
        if (conn?.LastSyncAt == null) return Array.Empty<SyncConflictDto>();

        var conflicts = new List<SyncConflictDto>();
        var sinceSync = conn.LastSyncAt.Value;

        var staleCustomers = await _db.Customers
            .Where(c => c.TenantId == tenantId && c.UpdatedAt > sinceSync)
            .Select(c => new { c.Id, c.Email, c.UpdatedAt })
            .Take(50)
            .ToListAsync(cancellationToken);

        foreach (var c in staleCustomers)
        {
            if (conn.Settings.TryGetValue($"ext:{c.Id}", out var extUpdated)
                && DateTime.TryParse(extUpdated, out var extDt)
                && extDt < c.UpdatedAt)
            {
                conflicts.Add(new SyncConflictDto(
                    provider, "Customer", c.Id.ToString(), "LocalNewer",
                    $"Local update {c.UpdatedAt:O} after external {extDt:O}"));
            }
        }

        if (staleCustomers.Count > 0 && conflicts.Count == 0)
        {
            conflicts.Add(new SyncConflictDto(
                provider, "Customer", staleCustomers[0].Id.ToString(), "PotentialDrift",
                $"{staleCustomers.Count} records changed locally since last sync"));
        }

        return conflicts;
    }
}
