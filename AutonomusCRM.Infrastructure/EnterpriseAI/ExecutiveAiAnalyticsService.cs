using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class ExecutiveAiAnalyticsService : IExecutiveAiAnalyticsService
{
    private readonly IModelRegistryService _registry;
    private readonly IAiEvaluationFrameworkService _evaluation;
    private readonly IMlOpsFoundationService _mlOps;
    private readonly IMachineLearningPipelineService _pipeline;
    private readonly IBusinessKnowledgeGraphEdgeRepository _graphEdges;
    private readonly IAiDecisionAuditRepository _audits;

    public ExecutiveAiAnalyticsService(
        IModelRegistryService registry,
        IAiEvaluationFrameworkService evaluation,
        IMlOpsFoundationService mlOps,
        IMachineLearningPipelineService pipeline,
        IBusinessKnowledgeGraphEdgeRepository graphEdges,
        IAiDecisionAuditRepository audits)
    {
        _registry = registry;
        _evaluation = evaluation;
        _mlOps = mlOps;
        _pipeline = pipeline;
        _graphEdges = graphEdges;
        _audits = audits;
    }

    public async Task<ExecutiveAiAnalyticsDto> GetAnalyticsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var activeModels = new List<MlModelVersionDto>();
        foreach (var mt in EnterpriseAiConstants.AllModelTypes)
        {
            var m = await _registry.GetActiveVersionAsync(tenantId, mt, cancellationToken);
            if (m != null) activeModels.Add(m);
        }

        var performance = await _evaluation.EvaluateAllAsync(tenantId, cancellationToken);
        var drift = await _mlOps.DetectDriftAsync(tenantId, cancellationToken);
        var pipelines = await _pipeline.GetStatusAsync(tenantId, cancellationToken);

        var audits = (await _audits.GetByTenantAsync(tenantId, 500, cancellationToken)).ToList();
        var last30 = audits.Count(a => a.CreatedAt >= DateTime.UtcNow.AddDays(-30));
        var avgPrecision = performance.Count > 0 ? performance.Average(p => p.Precision) : 0;
        var roi = performance.Sum(p => p.EstimatedRoiPercent);
        var churnRed = performance.Where(p => p.ModelType == EnterpriseAiConstants.ModelChurn).Select(p => p.ChurnImpactReduction).FirstOrDefault();
        var revLift = performance.Where(p => p.ModelType == EnterpriseAiConstants.ModelRevenue).Select(p => p.RevenueImpactEstimate).FirstOrDefault();

        var edgeCount = await _graphEdges.CountByTenantAsync(tenantId, cancellationToken);
        var graphSummary = new KnowledgeGraphSummaryDto(
            Math.Min(edgeCount * 2, 500), edgeCount, edgeCount / 2, edgeCount / 4);

        return new ExecutiveAiAnalyticsDto(
            activeModels, performance, drift, pipelines,
            roi, churnRed, revLift, last30, avgPrecision, graphSummary);
    }
}
