using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class MlOpsFoundationService : IMlOpsFoundationService
{
    private readonly IMlModelVersionRepository _models;
    private readonly IMlFeatureSnapshotRepository _snapshots;
    private readonly IMlDriftReportRepository _driftReports;
    private readonly IUnitOfWork _unitOfWork;

    public MlOpsFoundationService(
        IMlModelVersionRepository models,
        IMlFeatureSnapshotRepository snapshots,
        IMlDriftReportRepository driftReports,
        IUnitOfWork unitOfWork)
    {
        _models = models;
        _snapshots = snapshots;
        _driftReports = driftReports;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<ModelDriftDto>> DetectDriftAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var reports = new List<ModelDriftDto>();
        foreach (var modelType in EnterpriseAiConstants.AllModelTypes)
        {
            var active = await _models.GetActiveAsync(tenantId, modelType, cancellationToken);
            if (active == null) continue;

            var recent = (await _snapshots.GetByDatasetAsync(tenantId, modelType, 100, cancellationToken)).ToList();
            var baseline = (await _snapshots.GetByDatasetAsync(tenantId, modelType, 500, cancellationToken))
                .Skip(100).Take(200).ToList();
            if (recent.Count < 10 || baseline.Count < 10) continue;

            var drift = ComputeFeatureDrift(recent, baseline);
            var report = MlDriftReport.Capture(tenantId, modelType, drift, new Dictionary<string, object>
            {
                ["recent_samples"] = recent.Count,
                ["baseline_samples"] = baseline.Count
            });
            await _driftReports.AddAsync(report, cancellationToken);
            reports.Add(new ModelDriftDto(modelType, drift, report.AlertTriggered, report.MeasuredAt));
        }

        if (reports.Count > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        return reports;
    }

    public async Task MonitorModelsAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await DetectDriftAsync(tenantId, cancellationToken);

    private static double ComputeFeatureDrift(List<MlFeatureSnapshot> recent, List<MlFeatureSnapshot> baseline)
    {
        var recentAvg = AverageVector(recent);
        var baseAvg = AverageVector(baseline);
        var diff = 0.0;
        for (var i = 0; i < recentAvg.Length; i++)
            diff += Math.Abs(recentAvg[i] - baseAvg[i]);
        return Math.Round(diff / recentAvg.Length * 100, 2);
    }

    private static double[] AverageVector(List<MlFeatureSnapshot> samples)
    {
        if (samples.Count == 0) return [];
        var sum = MlFeatureExtractor.ToVector(samples[0].Features);
        for (var i = 1; i < samples.Count; i++)
        {
            var v = MlFeatureExtractor.ToVector(samples[i].Features);
            for (var j = 0; j < sum.Length; j++) sum[j] += v[j];
        }
        return sum.Select(x => x / samples.Count).ToArray();
    }
}
