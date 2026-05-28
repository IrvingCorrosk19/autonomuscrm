using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence.Repositories;

public class MlModelVersionRepository : Repository<MlModelVersion>, IMlModelVersionRepository
{
    public MlModelVersionRepository(ApplicationDbContext context) : base(context) { }

    public async Task<MlModelVersion?> GetActiveAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default)
        => await _dbSet.FirstOrDefaultAsync(m =>
            m.TenantId == tenantId && m.ModelType == modelType && m.Status == EnterpriseAiConstants.ModelStatusActive,
            cancellationToken);

    public async Task<IEnumerable<MlModelVersion>> GetByTypeAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default)
        => await _dbSet.Where(m => m.TenantId == tenantId && m.ModelType == modelType)
            .OrderByDescending(m => m.TrainedAt).ToListAsync(cancellationToken);
}

public class MlPipelineRunRepository : Repository<MlPipelineRun>, IMlPipelineRunRepository
{
    public MlPipelineRunRepository(ApplicationDbContext context) : base(context) { }

    public async Task<MlPipelineRun?> GetLatestAsync(Guid tenantId, string datasetType, CancellationToken cancellationToken = default)
        => await _dbSet.Where(r => r.TenantId == tenantId && r.DatasetType == datasetType)
            .OrderByDescending(r => r.StartedAt).FirstOrDefaultAsync(cancellationToken);
}

public class MlDriftReportRepository : Repository<MlDriftReport>, IMlDriftReportRepository
{
    public MlDriftReportRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<MlDriftReport>> GetRecentAsync(Guid tenantId, int take = 20, CancellationToken cancellationToken = default)
        => await _dbSet.Where(r => r.TenantId == tenantId).OrderByDescending(r => r.MeasuredAt).Take(take).ToListAsync(cancellationToken);
}

public class BusinessKnowledgeGraphEdgeRepository : Repository<BusinessKnowledgeGraphEdge>, IBusinessKnowledgeGraphEdgeRepository
{
    public BusinessKnowledgeGraphEdgeRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<BusinessKnowledgeGraphEdge>> GetByTenantAsync(Guid tenantId, int take = 2000, CancellationToken cancellationToken = default)
        => await _dbSet.Where(e => e.TenantId == tenantId).Take(take).ToListAsync(cancellationToken);

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await _dbSet.CountAsync(e => e.TenantId == tenantId, cancellationToken);
}

public class NbaOutcomeRecordRepository : Repository<NbaOutcomeRecord>, INbaOutcomeRecordRepository
{
    public NbaOutcomeRecordRepository(ApplicationDbContext context) : base(context) { }

    public async Task<IEnumerable<NbaOutcomeRecord>> GetRecentAsync(Guid tenantId, int take = 500, CancellationToken cancellationToken = default)
        => await _dbSet.Where(r => r.TenantId == tenantId).OrderByDescending(r => r.RecordedAt).Take(take).ToListAsync(cancellationToken);
}
