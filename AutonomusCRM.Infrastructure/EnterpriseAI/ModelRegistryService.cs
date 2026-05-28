using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class ModelRegistryService : IModelRegistryService
{
    private readonly IMlModelVersionRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public ModelRegistryService(IMlModelVersionRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<MlModelVersionDto?> GetActiveVersionAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default)
    {
        var m = await _repository.GetActiveAsync(tenantId, modelType, cancellationToken);
        return m == null ? null : ToDto(m);
    }

    public async Task<IReadOnlyList<MlModelVersionDto>> ListVersionsAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByTypeAsync(tenantId, modelType, cancellationToken);
        return list.Select(ToDto).ToList();
    }

    public async Task<bool> RollbackAsync(Guid tenantId, string modelType, string versionTag, CancellationToken cancellationToken = default)
    {
        var versions = (await _repository.GetByTypeAsync(tenantId, modelType, cancellationToken)).ToList();
        var target = versions.FirstOrDefault(v => v.VersionTag == versionTag);
        if (target == null) return false;

        foreach (var v in versions.Where(v => v.Status == EnterpriseAiConstants.ModelStatusActive))
            v.Archive();

        target.Activate();
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MlModelVersion> RegisterTrainedModelAsync(
        Guid tenantId, string modelType, Dictionary<string, object> weights,
        Dictionary<string, object> metrics, int sampleCount, CancellationToken cancellationToken = default)
    {
        var existing = (await _repository.GetByTypeAsync(tenantId, modelType, cancellationToken)).ToList();
        foreach (var v in existing.Where(v => v.Status == EnterpriseAiConstants.ModelStatusActive))
            v.Archive();

        var nextVersion = existing.Count == 0 ? "v1" : $"v{existing.Count + 1}";
        var model = MlModelVersion.Create(tenantId, modelType, nextVersion, weights, metrics, sampleCount);
        model.Activate();
        await _repository.AddAsync(model, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return model;
    }

    private static MlModelVersionDto ToDto(MlModelVersion m) => new(
        m.Id, m.ModelType, m.VersionTag, m.Status, m.TrainingSampleCount, m.TrainedAt, m.Metrics);
}
