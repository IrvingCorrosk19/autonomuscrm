using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class CustomerInsightsAgentService : ICustomerInsightsAgentService
{
    private readonly ICustomerInsightsEngine _insightsEngine;
    private readonly IChurnPredictionV2 _churnPrediction;
    private readonly IExpansionIntelligence _expansionIntel;
    private readonly IOperationalTaskService _taskService;
    private readonly ICustomerPlaybookService _playbooks;

    public CustomerInsightsAgentService(
        ICustomerInsightsEngine insightsEngine,
        IChurnPredictionV2 churnPrediction,
        IExpansionIntelligence expansionIntel,
        IOperationalTaskService taskService,
        ICustomerPlaybookService playbooks)
    {
        _insightsEngine = insightsEngine;
        _churnPrediction = churnPrediction;
        _expansionIntel = expansionIntel;
        _taskService = taskService;
        _playbooks = playbooks;
    }

    public async Task<IReadOnlyList<InsightActionDto>> AnalyzeAndActAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var actions = new List<InsightActionDto>();
        var insights = await _insightsEngine.GenerateInsightsAsync(tenantId, cancellationToken);

        foreach (var insight in insights.Where(i => i.Actionable && i.Severity is "High" or "Medium"))
        {
            if (!insight.CustomerId.HasValue)
                continue;

            var taskType = insight.Severity == "High"
                ? IntelligenceConstants.TaskAnomaly
                : IntelligenceConstants.TaskInsight;

            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Customer", insight.CustomerId.Value, taskType, cancellationToken))
                continue;

            await _taskService.CreateTaskAsync(
                tenantId,
                $"Insight: {insight.Title}",
                insight.Description,
                "Customer",
                insight.CustomerId.Value,
                null,
                DateTime.UtcNow.AddDays(insight.Severity == "High" ? 2 : 5),
                insight.Severity == "High" ? "Urgent" : "High",
                taskType,
                cancellationToken);

            actions.Add(new InsightActionDto(
                "CustomerInsightsAgent", insight.CustomerId, insight.InsightType,
                insight.Description, true));
        }

        foreach (var churn in (await _churnPrediction.PredictAsync(tenantId, cancellationToken: cancellationToken))
                     .Where(c => c.ChurnProbability >= 75))
        {
            await _playbooks.ExecutePlaybookAsync(tenantId, churn.CustomerId, CustomerSuccessConstants.PlaybookRescue, cancellationToken: cancellationToken);
            actions.Add(new InsightActionDto(
                "CustomerInsightsAgent", churn.CustomerId, "ChurnPrediction",
                $"Churn probability {churn.ChurnProbability}% — Rescue playbook", true));
        }

        foreach (var exp in (await _expansionIntel.AnalyzeAsync(tenantId, cancellationToken))
                     .Where(e => e.ReadinessLevel == "Ready").Take(5))
        {
            if (await _taskService.ExistsOpenTaskAsync(tenantId, "Customer", exp.CustomerId, IntelligenceConstants.TaskRecommendation, cancellationToken))
                continue;

            await _taskService.CreateTaskAsync(
                tenantId,
                $"Expansión lista: {exp.CustomerName}",
                exp.Recommendation,
                "Customer",
                exp.CustomerId,
                null,
                DateTime.UtcNow.AddDays(7),
                "Normal",
                IntelligenceConstants.TaskRecommendation,
                cancellationToken);

            actions.Add(new InsightActionDto(
                "CustomerInsightsAgent", exp.CustomerId, "ExpansionReady", exp.Recommendation, true));
        }

        return actions;
    }
}
