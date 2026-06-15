using System.Text.Json;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncRollbackService : IDbSyncRollbackService
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly ApplicationDbContext _db;

    public DbSyncRollbackService(ApplicationDbContext db) => _db = db;

    public DbSyncRollbackSnapshot CreateSnapshot(
        Guid jobId, Guid tenantId, int rowNumber, string entityType, Guid entityId, string action,
        Dictionary<string, object?>? previousState = null) => new()
    {
        Id = Guid.NewGuid(),
        JobId = jobId,
        TenantId = tenantId,
        RowNumber = rowNumber,
        EntityType = entityType,
        EntityId = entityId,
        Action = action,
        PreviousStateJson = JsonSerializer.Serialize(previousState ?? new Dictionary<string, object?>(), JsonOptions),
        CreatedAtUtc = DateTime.UtcNow
    };

    public async Task<DbSyncRollbackResultDto> ExecuteRollbackAsync(
        Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _db.DbSyncJobs.FirstOrDefaultAsync(j => j.TenantId == tenantId && j.Id == jobId, cancellationToken)
            ?? throw new KeyNotFoundException("Sync job not found.");

        var snapshots = await _db.DbSyncRollbackSnapshots
            .Where(s => s.TenantId == tenantId && s.JobId == jobId && !s.RolledBack)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ToListAsync(cancellationToken);

        var deleted = 0;
        var restored = 0;

        foreach (var snap in snapshots)
        {
            if (snap.Action == "Created")
            {
                if (await DeleteEntityAsync(tenantId, snap.EntityType, snap.EntityId, cancellationToken))
                    deleted++;
            }
            else if (snap.Action == "Updated")
            {
                if (await RestoreEntityAsync(tenantId, snap, cancellationToken))
                    restored++;
            }

            snap.RolledBack = true;
        }

        job.Status = DbSyncJobStatus.RolledBack;
        job.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);

        return new DbSyncRollbackResultDto(jobId, deleted, restored, DbSyncJobStatus.RolledBack);
    }

    private async Task<bool> DeleteEntityAsync(Guid tenantId, string entityType, Guid entityId, CancellationToken ct)
    {
        return entityType switch
        {
            "Customer" => await DeleteIfExists(_db.Customers, tenantId, entityId, ct),
            "Lead" => await DeleteIfExists(_db.Leads, tenantId, entityId, ct),
            "Deal" => await DeleteIfExists(_db.Deals, tenantId, entityId, ct),
            _ => false
        };
    }

    private static async Task<bool> DeleteIfExists<T>(
        DbSet<T> set, Guid tenantId, Guid id, CancellationToken ct) where T : class
    {
        var entity = await set.FindAsync([id], ct);
        if (entity == null) return false;
        set.Remove(entity);
        return true;
    }

    private async Task<bool> RestoreEntityAsync(Guid tenantId, DbSyncRollbackSnapshot snap, CancellationToken ct)
    {
        var state = JsonSerializer.Deserialize<Dictionary<string, object?>>(snap.PreviousStateJson, JsonOptions)
                    ?? new Dictionary<string, object?>();

        return snap.EntityType switch
        {
            "Customer" => await RestoreCustomerAsync(tenantId, snap.EntityId, state, ct),
            "Lead" => await RestoreLeadAsync(tenantId, snap.EntityId, state, ct),
            _ => false
        };
    }

    private async Task<bool> RestoreCustomerAsync(Guid tenantId, Guid id, Dictionary<string, object?> state, CancellationToken ct)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Id == id, ct);
        if (customer == null) return false;
        customer.UpdateContactInfo(
            state.GetValueOrDefault("Email")?.ToString(),
            state.GetValueOrDefault("Phone")?.ToString(),
            state.GetValueOrDefault("Company")?.ToString());
        return true;
    }

    private async Task<bool> RestoreLeadAsync(Guid tenantId, Guid id, Dictionary<string, object?> state, CancellationToken ct)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.TenantId == tenantId && l.Id == id, ct);
        if (lead == null) return false;
        lead.UpdateInfo(
            state.GetValueOrDefault("Name")?.ToString() ?? lead.Name,
            state.GetValueOrDefault("Email")?.ToString(),
            state.GetValueOrDefault("Phone")?.ToString(),
            state.GetValueOrDefault("Company")?.ToString(),
            LeadSource.Other);
        return true;
    }

    internal static Dictionary<string, object?> CaptureCustomerState(Customer c) => new()
    {
        ["Email"] = c.Email,
        ["Phone"] = c.Phone,
        ["Company"] = c.Company
    };

    internal static Dictionary<string, object?> CaptureLeadState(Lead l) => new()
    {
        ["Name"] = l.Name,
        ["Email"] = l.Email,
        ["Phone"] = l.Phone,
        ["Company"] = l.Company
    };
}
