using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class WorkflowTaskRepository : Repository<WorkflowTask>, IWorkflowTaskRepository
{
    public WorkflowTaskRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WorkflowTask>> GetOpenByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(t => t.TenantId == tenantId && t.Status == "Open")
            .OrderByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<WorkflowTask>> GetByTenantAsync(
        Guid tenantId,
        string? status = null,
        Guid? assignedToUserId = null,
        bool? overdueOnly = null,
        string? priority = null,
        CancellationToken cancellationToken = default)
    {
        var query = _dbSet.Where(t => t.TenantId == tenantId);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);

        if (assignedToUserId.HasValue)
            query = query.Where(t => t.AssignedToUserId == assignedToUserId.Value);

        if (!string.IsNullOrWhiteSpace(priority))
            query = query.Where(t => t.Priority == priority);

        if (overdueOnly == true)
        {
            var now = DateTime.UtcNow;
            query = query.Where(t => t.Status == "Open" && t.DueDate != null && t.DueDate < now);
        }

        return await query
            .OrderBy(t => t.DueDate ?? DateTime.MaxValue)
            .ThenByDescending(t => t.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> ExistsOpenTaskAsync(
        Guid tenantId,
        string relatedEntityType,
        Guid relatedEntityId,
        string taskType,
        CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(
            t => t.TenantId == tenantId
                 && t.Status == "Open"
                 && t.RelatedEntityType == relatedEntityType
                 && t.RelatedEntityId == relatedEntityId
                 && t.TaskType == taskType,
            cancellationToken);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbSet.AsNoTracking().Where(t => t.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(t => t.Status == status);
        return await query.CountAsync(cancellationToken);
    }

    public async Task<int> CountOverdueOpenAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _dbSet.AsNoTracking().CountAsync(
            t => t.TenantId == tenantId && t.Status == "Open" && t.DueDate != null && t.DueDate < now,
            cancellationToken);
    }

    public async Task<IReadOnlyDictionary<Guid, CustomerTaskHealthAggregate>> GetHealthTaskAggregatesByCustomerAsync(
        Guid tenantId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var direct = await _dbSet.AsNoTracking()
            .Where(t => t.TenantId == tenantId
                        && t.RelatedEntityType == "Customer"
                        && t.RelatedEntityId != null)
            .GroupBy(t => t.RelatedEntityId!.Value)
            .Select(g => new CustomerTaskHealthAggregate(
                g.Key,
                g.Count(t => t.TaskType != null && EF.Functions.Like(t.TaskType, "Onboarding_%")),
                g.Count(t => t.TaskType != null
                             && EF.Functions.Like(t.TaskType, "Onboarding_%")
                             && t.Status == "Completed"),
                g.Count(t => t.Status == "Open"),
                g.Count(t => t.Status == "Open" && t.DueDate != null && t.DueDate < now)))
            .ToListAsync(cancellationToken);

        var dealLinked = await (
                from t in _dbSet.AsNoTracking()
                join d in _context.Set<Deal>().AsNoTracking() on t.RelatedEntityId equals d.Id
                where t.TenantId == tenantId
                      && t.RelatedEntityType == "Deal"
                      && t.RelatedEntityId != null
                      && d.TenantId == tenantId
                group t by d.CustomerId
                into g
                select new CustomerTaskHealthAggregate(
                    g.Key,
                    g.Count(t => t.TaskType != null && EF.Functions.Like(t.TaskType, "Onboarding_%")),
                    g.Count(t => t.TaskType != null
                                 && EF.Functions.Like(t.TaskType, "Onboarding_%")
                                 && t.Status == "Completed"),
                    g.Count(t => t.Status == "Open"),
                    g.Count(t => t.Status == "Open" && t.DueDate != null && t.DueDate < now)))
            .ToListAsync(cancellationToken);

        return MergeTaskAggregates(direct, dealLinked);
    }

    public async Task<CustomerTaskHealthAggregate> GetHealthTaskAggregateForCustomerAsync(
        Guid tenantId,
        Guid customerId,
        CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var direct = await _dbSet.AsNoTracking()
            .Where(t => t.TenantId == tenantId
                        && t.RelatedEntityType == "Customer"
                        && t.RelatedEntityId == customerId)
            .GroupBy(_ => 1)
            .Select(g => new CustomerTaskHealthAggregate(
                customerId,
                g.Count(t => t.TaskType != null && EF.Functions.Like(t.TaskType, "Onboarding_%")),
                g.Count(t => t.TaskType != null
                             && EF.Functions.Like(t.TaskType, "Onboarding_%")
                             && t.Status == "Completed"),
                g.Count(t => t.Status == "Open"),
                g.Count(t => t.Status == "Open" && t.DueDate != null && t.DueDate < now)))
            .FirstOrDefaultAsync(cancellationToken);

        var dealLinked = await (
                from t in _dbSet.AsNoTracking()
                join d in _context.Set<Deal>().AsNoTracking() on t.RelatedEntityId equals d.Id
                where t.TenantId == tenantId
                      && t.RelatedEntityType == "Deal"
                      && t.RelatedEntityId != null
                      && d.TenantId == tenantId
                      && d.CustomerId == customerId
                group t by 1
                into g
                select new CustomerTaskHealthAggregate(
                    customerId,
                    g.Count(t => t.TaskType != null && EF.Functions.Like(t.TaskType, "Onboarding_%")),
                    g.Count(t => t.TaskType != null
                                 && EF.Functions.Like(t.TaskType, "Onboarding_%")
                                 && t.Status == "Completed"),
                    g.Count(t => t.Status == "Open"),
                    g.Count(t => t.Status == "Open" && t.DueDate != null && t.DueDate < now)))
            .FirstOrDefaultAsync(cancellationToken);

        if (direct == null && dealLinked == null)
            return new CustomerTaskHealthAggregate(customerId, 0, 0, 0, 0);

        if (direct == null)
            return dealLinked!;
        if (dealLinked == null)
            return direct;

        return new CustomerTaskHealthAggregate(
            customerId,
            direct.OnboardingTotal + dealLinked.OnboardingTotal,
            direct.OnboardingCompleted + dealLinked.OnboardingCompleted,
            direct.OpenTaskCount + dealLinked.OpenTaskCount,
            direct.OverdueOpenCount + dealLinked.OverdueOpenCount);
    }

    private static Dictionary<Guid, CustomerTaskHealthAggregate> MergeTaskAggregates(
        IReadOnlyList<CustomerTaskHealthAggregate> direct,
        IReadOnlyList<CustomerTaskHealthAggregate> dealLinked)
    {
        var merged = new Dictionary<Guid, CustomerTaskHealthAggregate>();
        foreach (var row in direct.Concat(dealLinked))
        {
            if (merged.TryGetValue(row.CustomerId, out var existing))
            {
                merged[row.CustomerId] = new CustomerTaskHealthAggregate(
                    row.CustomerId,
                    existing.OnboardingTotal + row.OnboardingTotal,
                    existing.OnboardingCompleted + row.OnboardingCompleted,
                    existing.OpenTaskCount + row.OpenTaskCount,
                    existing.OverdueOpenCount + row.OverdueOpenCount);
            }
            else
            {
                merged[row.CustomerId] = row;
            }
        }

        return merged;
    }
}
