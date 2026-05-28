using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class MachineLearningPipelineService : IMachineLearningPipelineService
{
    private static readonly Dictionary<string, string> PositiveLabels = new()
    {
        [EnterpriseAiConstants.ModelChurn] = "churned",
        [EnterpriseAiConstants.ModelExpansion] = "expansion_ready",
        [EnterpriseAiConstants.ModelRevenue] = "high_revenue",
        [EnterpriseAiConstants.ModelNba] = "converted",
        [EnterpriseAiConstants.ModelRenewal] = "renewed"
    };

    private readonly IMlFeatureSnapshotRepository _snapshots;
    private readonly IMlPipelineRunRepository _pipelineRuns;
    private readonly IModelRegistryService _registry;
    private readonly IUnitOfWork _unitOfWork;

    public MachineLearningPipelineService(
        IMlFeatureSnapshotRepository snapshots,
        IMlPipelineRunRepository pipelineRuns,
        IModelRegistryService registry,
        IUnitOfWork unitOfWork)
    {
        _snapshots = snapshots;
        _pipelineRuns = pipelineRuns;
        _registry = registry;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<MlPipelineStatusDto>> GetStatusAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = new List<MlPipelineStatusDto>();
        foreach (var ds in EnterpriseAiConstants.AllModelTypes)
        {
            var count = await _snapshots.CountByDatasetAsync(tenantId, ds, cancellationToken);
            var active = await _registry.GetActiveVersionAsync(tenantId, ds, cancellationToken);
            var latest = await _pipelineRuns.GetLatestAsync(tenantId, ds, cancellationToken);
            result.Add(new MlPipelineStatusDto(ds, count, active?.VersionTag, latest?.CompletedAt, latest?.Status ?? "idle"));
        }
        return result;
    }

    public async Task<IReadOnlyList<MlTrainResultDto>> TrainAllAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var results = new List<MlTrainResultDto>();
        foreach (var mt in EnterpriseAiConstants.AllModelTypes)
            results.Add(await TrainModelAsync(tenantId, mt, cancellationToken));
        return results;
    }

    public async Task<MlTrainResultDto> TrainModelAsync(Guid tenantId, string modelType, CancellationToken cancellationToken = default)
    {
        var run = MlPipelineRun.Start(tenantId, modelType);
        await _pipelineRuns.AddAsync(run, cancellationToken);

        try
        {
            var samples = (await _snapshots.GetByDatasetAsync(tenantId, modelType, 1000, cancellationToken)).ToList();
            if (samples.Count < EnterpriseAiConstants.MinTrainingSamples)
            {
                run.Fail($"Insufficient samples ({samples.Count}/{EnterpriseAiConstants.MinTrainingSamples})");
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                var msg = run.RunMetrics.TryGetValue("error", out var err) ? err?.ToString() : "Insufficient samples";
                return new MlTrainResultDto(modelType, "", false, samples.Count, new(), msg);
            }

            var positive = PositiveLabels.GetValueOrDefault(modelType, "positive");
            var (x, y) = MlFeatureExtractor.BuildMatrix(samples, positive);
            var trained = LogisticRegressionTrainer.Train(x, y);
            var weights = MlFeatureExtractor.WeightsToDict(trained.Weights, trained.Bias);
            var metrics = new Dictionary<string, object>
            {
                ["precision"] = trained.Precision,
                ["recall"] = trained.Recall,
                ["f1"] = trained.F1,
                ["accuracy"] = trained.Accuracy
            };

            var model = await _registry.RegisterTrainedModelAsync(tenantId, modelType, weights, metrics, samples.Count, cancellationToken);
            run.Complete(samples.Count, model.VersionTag, metrics);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            return new MlTrainResultDto(modelType, model.VersionTag, true, samples.Count, metrics, null);
        }
        catch (Exception ex)
        {
            run.Fail(ex.Message);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return new MlTrainResultDto(modelType, "", false, 0, new(), ex.Message);
        }
    }
}
