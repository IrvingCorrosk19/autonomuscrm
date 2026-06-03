using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataPlatform;

public sealed class IdentityResolutionService : IIdentityResolutionService
{
    private readonly ApplicationDbContext _db;

    public IdentityResolutionService(ApplicationDbContext db) => _db = db;

    public async Task<IReadOnlyList<IdentityDuplicateGroupDto>> FindDuplicatesByEmailAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var customers = await _db.Customers
            .Where(c => c.TenantId == tenantId && c.Email != null && c.Email != "")
            .Select(c => new { c.Id, c.Email })
            .ToListAsync(cancellationToken);

        return customers
            .GroupBy(c => c.Email!.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .Select(g =>
            {
                var ids = g.Select(x => x.Id).ToList();
                return new IdentityDuplicateGroupDto(g.Key, ids[0], ids.Skip(1).ToList());
            })
            .ToList();
    }

    public async Task<Guid?> ResolveCanonicalCustomerIdAsync(
        Guid tenantId, string? email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email)) return null;
        var normalized = email.Trim().ToLowerInvariant();
        return await _db.Customers
            .Where(c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == normalized)
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
