using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class AiDecisionAuditRepository : Repository<AiDecisionAudit>, IAiDecisionAuditRepository
{
    public AiDecisionAuditRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<AiDecisionAudit>> GetByTenantAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default)
        => await _dbSet.Where(a => a.TenantId == tenantId).OrderByDescending(a => a.CreatedAt).Take(take).ToListAsync(cancellationToken);
}

public class AutonomousPlaybookStateRepository : Repository<AutonomousPlaybookState>, IAutonomousPlaybookStateRepository
{
    public AutonomousPlaybookStateRepository(ApplicationDbContext context) : base(context) { }

    public async Task<AutonomousPlaybookState?> GetActiveAsync(
        Guid tenantId, Guid customerId, string playbookType, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(s =>
            s.TenantId == tenantId && s.CustomerId == customerId && s.PlaybookType == playbookType
            && s.Status == AutonomousConstants.PlaybookStateActive, cancellationToken);
}

public class BusinessKnowledgeRepository : Repository<BusinessKnowledgeRecord>, IBusinessKnowledgeRepository
{
    public BusinessKnowledgeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<BusinessKnowledgeRecord?> GetByPatternAsync(
        Guid tenantId, string patternKey, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(k => k.TenantId == tenantId && k.PatternKey == patternKey, cancellationToken);
}

public class MlFeatureSnapshotRepository : Repository<MlFeatureSnapshot>, IMlFeatureSnapshotRepository
{
    public MlFeatureSnapshotRepository(ApplicationDbContext context) : base(context) { }

    public async Task<int> CountByDatasetAsync(Guid tenantId, string datasetType, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(s => s.TenantId == tenantId && s.DatasetType == datasetType, cancellationToken);

    public async Task<IEnumerable<MlFeatureSnapshot>> GetByDatasetAsync(
        Guid tenantId, string datasetType, int take = 500, CancellationToken cancellationToken = default)
        => await _dbSet.Where(s => s.TenantId == tenantId && s.DatasetType == datasetType)
            .OrderByDescending(s => s.CapturedAt).Take(take).ToListAsync(cancellationToken);
}
