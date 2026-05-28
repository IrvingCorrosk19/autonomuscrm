using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class EnterpriseAiCycleService : IEnterpriseAiCycleService
{
    private readonly IMlFoundationService _mlFoundation;
    private readonly IMachineLearningPipelineService _pipeline;
    private readonly ISelfLearningEngine _selfLearning;
    private readonly IMlOpsFoundationService _mlOps;
    private readonly IBusinessKnowledgeGraphService _graph;
    private readonly IAutonomousOptimizationEngine _optimization;
    private readonly ILogger<EnterpriseAiCycleService> _logger;

    public EnterpriseAiCycleService(
        IMlFoundationService mlFoundation,
        IMachineLearningPipelineService pipeline,
        ISelfLearningEngine selfLearning,
        IMlOpsFoundationService mlOps,
        IBusinessKnowledgeGraphService graph,
        IAutonomousOptimizationEngine optimization,
        ILogger<EnterpriseAiCycleService> logger)
    {
        _mlFoundation = mlFoundation;
        _pipeline = pipeline;
        _selfLearning = selfLearning;
        _mlOps = mlOps;
        _graph = graph;
        _optimization = optimization;
        _logger = logger;
    }

    public async Task RunEnterpriseAiCycleAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var samples = await _mlFoundation.CaptureTrainingSamplesAsync(tenantId, cancellationToken);
        var trained = await _pipeline.TrainAllAsync(tenantId, cancellationToken);
        var learning = await _selfLearning.RunLearningCycleAsync(tenantId, cancellationToken);
        await _mlOps.MonitorModelsAsync(tenantId, cancellationToken);
        var edges = await _graph.RebuildGraphAsync(tenantId, cancellationToken);
        var opt = await _optimization.OptimizeTenantAsync(tenantId, cancellationToken);

        _logger.LogInformation(
            "Enterprise AI cycle tenant {TenantId}: {Samples} samples, {Trained} models trained, {Edges} graph edges, {Opt} optimizations",
            tenantId, samples, trained.Count(r => r.Success), edges, opt.Count);
    }
}
