using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataPlatform;

public sealed class IdentityMergeService : IIdentityMergeService
{
    private readonly ApplicationDbContext _db;
    private readonly IIdentityResolutionService _resolution;
    private readonly ICdpEventStreamService _events;

    public IdentityMergeService(
        ApplicationDbContext db,
        IIdentityResolutionService resolution,
        ICdpEventStreamService events)
    {
        _db = db;
        _resolution = resolution;
        _events = events;
    }

    public async Task<int> MergeDuplicatesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var groups = await _resolution.FindDuplicatesByEmailAsync(tenantId, cancellationToken);
        var merged = 0;

        foreach (var g in groups)
        {
            var canonicalId = g.CanonicalCustomerId;
            foreach (var dupId in g.DuplicateCustomerIds)
            {
                await _db.Deals
                    .Where(d => d.TenantId == tenantId && d.CustomerId == dupId)
                    .ExecuteUpdateAsync(s => s.SetProperty(d => d.CustomerId, canonicalId), cancellationToken);

                var dup = await _db.Customers.FirstOrDefaultAsync(c => c.Id == dupId, cancellationToken);
                if (dup != null)
                {
                    dup.ChangeStatus(CustomerStatus.Inactive);
                    dup.UpdateMetadata("mergedInto", canonicalId);
                    merged++;
                }

                await _events.PublishAsync(tenantId, "identity.merged",
                    canonicalId,
                    new Dictionary<string, object?> { ["duplicateId"] = dupId, ["email"] = g.NormalizedEmail },
                    cancellationToken);
            }
        }

        if (merged > 0)
            await _db.SaveChangesAsync(cancellationToken);

        return merged;
    }
}
