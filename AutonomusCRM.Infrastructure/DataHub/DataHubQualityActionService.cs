using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class NullDataHubProgressNotifier : IDataHubProgressNotifier
{
    public Task NotifyProgressAsync(DataHubProgressUpdateDto update, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

public sealed class DataHubQualityActionService : IDataHubQualityActionService
{
    private readonly ApplicationDbContext _db;

    public DataHubQualityActionService(ApplicationDbContext db) => _db = db;

    public async Task<DataHubQualityActionResultDto> MergeCustomersAsync(
        Guid tenantId, Guid keepId, IReadOnlyList<Guid> mergeIds, CancellationToken cancellationToken = default)
    {
        var keep = await _db.Customers.FirstOrDefaultAsync(c => c.Id == keepId && c.TenantId == tenantId, cancellationToken);
        if (keep == null) return new(false, "Record not found.", 0);

        var merged = 0;
        foreach (var id in mergeIds.Where(i => i != keepId))
        {
            var dup = await _db.Customers.FirstOrDefaultAsync(c => c.Id == id && c.TenantId == tenantId, cancellationToken);
            if (dup == null) continue;

            if (string.IsNullOrWhiteSpace(keep.Email) && !string.IsNullOrWhiteSpace(dup.Email))
                keep.UpdateContactInfo(dup.Email, keep.Phone ?? dup.Phone, keep.Company ?? dup.Company);
            else if (!string.IsNullOrWhiteSpace(dup.Phone) || !string.IsNullOrWhiteSpace(dup.Company))
                keep.UpdateContactInfo(keep.Email, keep.Phone ?? dup.Phone, keep.Company ?? dup.Company);

            _db.Customers.Remove(dup);
            merged++;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return new(true, $"Merged {merged} duplicate customer(s) into the primary record.", merged);
    }

    public async Task<DataHubQualityActionResultDto> AssignLeadOwnerAsync(
        Guid tenantId, Guid leadId, Guid userId, CancellationToken cancellationToken = default)
    {
        var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == leadId && l.TenantId == tenantId, cancellationToken);
        if (lead == null) return new(false, "Lead not found.", 0);

        var userExists = await _db.Users.AnyAsync(u => u.Id == userId && u.TenantId == tenantId, cancellationToken);
        if (!userExists) return new(false, "User not found.", 0);

        lead.AssignToUser(userId);
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Lead assigned successfully.", 1);
    }

    public async Task<DataHubQualityActionResultDto> AutoAssignLeadsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = await _db.Users
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.Email)
            .Select(u => u.Id)
            .ToListAsync(cancellationToken);
        if (users.Count == 0) return new(false, "No active users available for assignment.", 0);

        var leads = await _db.Leads
            .Where(l => l.TenantId == tenantId && l.AssignedToUserId == null)
            .Take(200)
            .ToListAsync(cancellationToken);
        if (leads.Count == 0) return new(true, "All leads already have an owner.", 0);

        for (var i = 0; i < leads.Count; i++)
            leads[i].AssignToUser(users[i % users.Count]);

        await _db.SaveChangesAsync(cancellationToken);
        return new(true, $"Auto-assigned {leads.Count} lead(s) across {users.Count} team member(s).", leads.Count);
    }

    public async Task<DataHubQualityActionResultDto> MarkForReviewAsync(
        Guid tenantId, string entityType, Guid entityId, CancellationToken cancellationToken = default)
    {
        if (entityType == "Customer")
        {
            var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == entityId && c.TenantId == tenantId, cancellationToken);
            if (customer == null) return new(false, "Customer not found.", 0);
            customer.Metadata["dataHubReview"] = true;
            customer.Metadata["dataHubReviewAt"] = DateTime.UtcNow.ToString("O");
            await _db.SaveChangesAsync(cancellationToken);
            return new(true, "Customer marked for review.", 1);
        }

        if (entityType == "Lead")
        {
            var lead = await _db.Leads.FirstOrDefaultAsync(l => l.Id == entityId && l.TenantId == tenantId, cancellationToken);
            if (lead == null) return new(false, "Lead not found.", 0);
            lead.Metadata["dataHubReview"] = true;
            await _db.SaveChangesAsync(cancellationToken);
            return new(true, "Lead marked for review.", 1);
        }

        return new(false, "Unsupported entity type.", 0);
    }

    public async Task<DataHubQualityActionResultDto> KeepDuplicatesAsync(
        Guid tenantId, Guid entityId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == entityId && c.TenantId == tenantId, cancellationToken);
        if (customer == null) return new(false, "Customer not found.", 0);
        customer.Metadata["duplicateAcknowledged"] = true;
        await _db.SaveChangesAsync(cancellationToken);
        return new(true, "Duplicate acknowledged — records kept as separate.", 1);
    }
}
