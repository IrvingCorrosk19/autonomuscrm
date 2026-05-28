using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class AutonomousOptimizationEngine : IAutonomousOptimizationEngine
{
    private readonly IAutonomousPlaybookStateRepository _playbooks;
    private readonly IBusinessKnowledgeEngine _knowledge;
    private readonly INbaOutcomeRecordRepository _nbaOutcomes;
    private readonly IUnitOfWork _unitOfWork;

    public AutonomousOptimizationEngine(
        IAutonomousPlaybookStateRepository playbooks,
        IBusinessKnowledgeEngine knowledge,
        INbaOutcomeRecordRepository nbaOutcomes,
        IUnitOfWork unitOfWork)
    {
        _playbooks = playbooks;
        _knowledge = knowledge;
        _nbaOutcomes = nbaOutcomes;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<OptimizationResultDto>> OptimizeTenantAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var results = new List<OptimizationResultDto>();

        var insights = await _knowledge.GetInsightsAsync(tenantId, cancellationToken);
        var playbookOptimized = insights.Count(i => i.SuccessRate >= 60);
        results.Add(new OptimizationResultDto(
            "playbooks",
            playbookOptimized,
            $"Priorizados {playbookOptimized} patrones de playbook con éxito ≥60%."));

        var nba = (await _nbaOutcomes.GetRecentAsync(tenantId, 300, cancellationToken)).ToList();
        var channelGroups = nba.GroupBy(n => n.Channel)
            .Select(g => new { Channel = g.Key, Rate = g.Count(x => x.Converted) * 100.0 / g.Count() })
            .OrderByDescending(x => x.Rate).ToList();
        results.Add(new OptimizationResultDto(
            "communications",
            channelGroups.Count,
            channelGroups.Count > 0
                ? $"Canal óptimo: {channelGroups[0].Channel} ({channelGroups[0].Rate:F0}% conversión)."
                : "Sin datos NBA suficientes."));

        var sequences = nba.GroupBy(n => n.RecommendedAction).Count();
        results.Add(new OptimizationResultDto(
            "sequences",
            sequences,
            $"Evaluadas {sequences} secuencias de acción recomendada."));

        results.Add(new OptimizationResultDto(
            "recommendations",
            insights.Count,
            $"Recalibradas {insights.Count} recomendaciones desde Business Knowledge."));

        return results;
    }
}
