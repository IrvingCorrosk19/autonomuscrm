using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class ProductUsageEventRepository : Repository<ProductUsageEvent>, IProductUsageEventRepository
{
    public ProductUsageEventRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<ProductUsageEvent>> GetByTenantAsync(
        Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        var q = _dbSet.Where(e => e.TenantId == tenantId);
        if (from.HasValue) q = q.Where(e => e.RecordedAt >= from.Value);
        if (to.HasValue) q = q.Where(e => e.RecordedAt <= to.Value);
        return await q.OrderByDescending(e => e.RecordedAt).ToListAsync(cancellationToken);
    }
}

public class CustomerFeedbackRepository : Repository<CustomerFeedback>, ICustomerFeedbackRepository
{
    public CustomerFeedbackRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<CustomerFeedback>> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(f => f.TenantId == tenantId).OrderByDescending(f => f.SubmittedAt).ToListAsync(cancellationToken);

    public async Task<IEnumerable<CustomerFeedback>> GetByCustomerAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(f => f.TenantId == tenantId && f.CustomerId == customerId)
            .OrderByDescending(f => f.SubmittedAt).ToListAsync(cancellationToken);
}

public class CustomerAnalyticsSnapshotRepository : Repository<CustomerAnalyticsSnapshot>, ICustomerAnalyticsSnapshotRepository
{
    public CustomerAnalyticsSnapshotRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<CustomerAnalyticsSnapshot>> GetByTenantAsync(
        Guid tenantId, DateTime? from = null, CancellationToken cancellationToken = default)
    {
        var q = _dbSet.Where(s => s.TenantId == tenantId);
        if (from.HasValue) q = q.Where(s => s.SnapshotDate >= from.Value.Date);
        return await q.OrderByDescending(s => s.SnapshotDate).ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<CustomerAnalyticsSnapshot>> GetByCustomerAsync(
        Guid tenantId, Guid customerId, int take = 90, CancellationToken cancellationToken = default)
        => await _dbSet.Where(s => s.TenantId == tenantId && s.CustomerId == customerId)
            .OrderByDescending(s => s.SnapshotDate).Take(take).ToListAsync(cancellationToken);

    public async Task<CustomerAnalyticsSnapshot?> GetLatestAsync(
        Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
        => await _dbSet.Where(s => s.TenantId == tenantId && s.CustomerId == customerId)
            .OrderByDescending(s => s.SnapshotDate).FirstOrDefaultAsync(cancellationToken);
}
