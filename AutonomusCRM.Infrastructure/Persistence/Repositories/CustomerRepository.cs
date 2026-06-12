using AutonomusCRM.Application.Common;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class CustomerRepository : Repository<Customer>, ICustomerRepository
{
    public CustomerRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<Customer>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(c => c.TenantId == tenantId).ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Email == email, cancellationToken);
    }

    public async Task<IEnumerable<Customer>> GetByStatusAsync(Guid tenantId, CustomerStatus status, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().Where(c => c.TenantId == tenantId && c.Status == status).ToListAsync(cancellationToken);
    }

    public Task<PagedResult<Customer>> SearchPagedAsync(
        Guid tenantId,
        string? search,
        CustomerStatus? status,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        var query = ApplyFilters(_dbSet.AsNoTracking(), tenantId, search, status)
            .OrderByDescending(c => c.CreatedAt);
        return RepositoryPaging.ToPagedAsync(query, page, pageSize, cancellationToken);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().CountAsync(c => c.TenantId == tenantId, cancellationToken);
    }

    public async Task<CustomerListSummary> GetListSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(c => c.TenantId == tenantId);
        var total = await query.CountAsync(cancellationToken);
        var withLtv = query.Where(c => c.LifetimeValue != null);
        var avgLtv = await withLtv.AnyAsync(cancellationToken)
            ? await withLtv.AverageAsync(c => c.LifetimeValue!.Value, cancellationToken)
            : 0m;
        var highLtv = await query.CountAsync(c => c.LifetimeValue > 10000, cancellationToken);
        var highRisk = await query.CountAsync(c => c.RiskScore > 70, cancellationToken);
        var withRisk = query.Where(c => c.RiskScore != null);
        double? avgRisk = await withRisk.AnyAsync(cancellationToken)
            ? await withRisk.AverageAsync(c => (double)c.RiskScore!.Value, cancellationToken)
            : null;
        var lowRisk = await query.CountAsync(c => c.RiskScore < 30, cancellationToken);
        return new CustomerListSummary(total, avgLtv, highLtv, highRisk, avgRisk, lowRisk);
    }

    public async Task<CustomerJourneyCounts> GetJourneyCustomerCountsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var baseQuery = _dbSet.AsNoTracking().Where(c => c.TenantId == tenantId);
        var active = await baseQuery.CountAsync(
            c => c.Status == CustomerStatus.Customer || c.Status == CustomerStatus.VIP,
            cancellationToken);
        var onboarding = await PostgresJsonbQuery.CountJsonbKeyAsync(
            _context, "Customers", tenantId, "OnboardingStarted", cancellationToken);
        var renewal = await PostgresJsonbQuery.CountJsonbKeyAsync(
            _context, "Customers", tenantId, "RenewalInProgress", cancellationToken);
        var expansion = await PostgresJsonbQuery.CountJsonbKeyAsync(
            _context, "Customers", tenantId, "ExpansionOpportunity", cancellationToken);

        return new CustomerJourneyCounts(active, onboarding, renewal, expansion);
    }

    public async Task<IReadOnlyList<CustomerHealthProjection>> GetHealthEligibleProjectionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(c => c.TenantId == tenantId
                        && (c.Status == CustomerStatus.Customer
                            || c.Status == CustomerStatus.VIP
                            || c.Status == CustomerStatus.Qualified))
            .Select(c => new CustomerHealthProjection(
                c.Id,
                c.Name,
                c.LastContactAt,
                c.LifetimeValue,
                c.RiskScore))
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ExpansionCustomerProjection>> GetExpansionCustomerProjectionsAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var rows = await _dbSet.AsNoTracking()
            .Where(c => c.TenantId == tenantId
                        && (c.Status == CustomerStatus.Customer || c.Status == CustomerStatus.VIP))
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Status,
                ProductLine = c.Metadata.ContainsKey("ProductLine")
                    ? c.Metadata["ProductLine"].ToString()
                    : null
            })
            .ToListAsync(cancellationToken);

        return rows
            .Select(r => new ExpansionCustomerProjection(
                r.Id,
                r.Name,
                r.Status,
                r.ProductLine != null && r.ProductLine.Contains(',')))
            .ToList();
    }

    private static IQueryable<Customer> ApplyFilters(
        IQueryable<Customer> query,
        Guid tenantId,
        string? search,
        CustomerStatus? status)
    {
        query = query.Where(c => c.TenantId == tenantId);
        if (status.HasValue)
            query = query.Where(c => c.Status == status.Value);
        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = $"%{search.Trim()}%";
            query = query.Where(c =>
                EF.Functions.ILike(c.Name, pattern) ||
                (c.Email != null && EF.Functions.ILike(c.Email, pattern)) ||
                (c.Company != null && EF.Functions.ILike(c.Company, pattern)) ||
                (c.Phone != null && c.Phone.Contains(search.Trim())));
        }
        return query;
    }
}
