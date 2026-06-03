using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.SemanticMemory;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class EnterpriseAiCycleService : IEnterpriseAiCycleService
{
    private readonly IMlFoundationService _mlFoundation;
    private readonly IMachineLearningPipelineService _pipeline;
    private readonly ISelfLearningEngine _selfLearning;
    private readonly IMlOpsFoundationService _mlOps;
    private readonly Application.KnowledgeGraph.IKnowledgeGraphService _knowledgeGraph;
    private readonly IAutonomousOptimizationEngine _optimization;
    private readonly ISemanticMemoryService _semanticMemory;
    private readonly ILogger<EnterpriseAiCycleService> _logger;

    public EnterpriseAiCycleService(
        IMlFoundationService mlFoundation,
        IMachineLearningPipelineService pipeline,
        ISelfLearningEngine selfLearning,
        IMlOpsFoundationService mlOps,
        Application.KnowledgeGraph.IKnowledgeGraphService knowledgeGraph,
        IAutonomousOptimizationEngine optimization,
        ISemanticMemoryService semanticMemory,
        ILogger<EnterpriseAiCycleService> logger)
    {
        _mlFoundation = mlFoundation;
        _pipeline = pipeline;
        _selfLearning = selfLearning;
        _mlOps = mlOps;
        _knowledgeGraph = knowledgeGraph;
        _optimization = optimization;
        _semanticMemory = semanticMemory;
        _logger = logger;
    }

    public async Task RunEnterpriseAiCycleAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var samples = await _mlFoundation.CaptureTrainingSamplesAsync(tenantId, cancellationToken);
        var trained = await _pipeline.TrainAllAsync(tenantId, cancellationToken);
        var learning = await _selfLearning.RunLearningCycleAsync(tenantId, cancellationToken);
        await _mlOps.MonitorModelsAsync(tenantId, cancellationToken);
        var edges = await _knowledgeGraph.BuildGraphAsync(tenantId, cancellationToken);
        var opt = await _optimization.OptimizeTenantAsync(tenantId, cancellationToken);

        await _semanticMemory.IndexBusinessMemorySourcesAsync(tenantId, 40, cancellationToken);
        var consolidated = await _semanticMemory.ConsolidateTenantAsync(tenantId, cancellationToken);

        _logger.LogInformation(
            "Enterprise AI cycle tenant {TenantId}: {Samples} samples, {Trained} models trained, {Edges} graph edges, {Opt} optimizations, {Consolidated} consolidated patterns",
            tenantId, samples, trained.Count(r => r.Success), edges, opt.Count, consolidated);
    }
}
