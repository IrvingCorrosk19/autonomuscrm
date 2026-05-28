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
}
