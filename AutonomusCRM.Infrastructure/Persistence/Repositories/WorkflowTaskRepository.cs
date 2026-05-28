using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class WorkflowTaskRepository : Repository<WorkflowTask>, IWorkflowTaskRepository
{
    public WorkflowTaskRepository(ApplicationDbContext context) : base(context)
    {
    }

    public async Task<IEnumerable<WorkflowTask>> GetOpenByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await _dbSet
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
}
