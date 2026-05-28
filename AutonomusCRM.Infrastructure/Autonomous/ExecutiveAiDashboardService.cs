using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class ExecutiveAiDashboardService : IExecutiveAiDashboardService
{
    private readonly IAiDecisionAuditService _audit;
    private readonly IPredictiveRevenueEngine _predictive;
    private readonly INextBestActionEngine _nba;
    private readonly IBusinessKnowledgeEngine _knowledge;
    private readonly IChurnPredictionV2 _churn;
    private readonly IExpansionIntelligence _expansion;
    private readonly IAiDecisionAuditRepository _auditRepo;

    public ExecutiveAiDashboardService(
        IAiDecisionAuditService audit,
        IPredictiveRevenueEngine predictive,
        INextBestActionEngine nba,
        IBusinessKnowledgeEngine knowledge,
        IChurnPredictionV2 churn,
        IExpansionIntelligence expansion,
        IAiDecisionAuditRepository auditRepo)
    {
        _audit = audit;
        _predictive = predictive;
        _nba = nba;
        _knowledge = knowledge;
        _churn = churn;
        _expansion = expansion;
        _auditRepo = auditRepo;
    }

    public async Task<ExecutiveAiDashboardDto> GetDashboardAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var allAudits = (await _auditRepo.GetByTenantAsync(tenantId, 200, cancellationToken)).ToList();
        var today = DateTime.UtcNow.Date;
        var pending = allAudits.Count(a => a.Status == AutonomousConstants.AuditPending);
        var executedToday = allAudits.Count(a => a.ExecutedAt?.Date == today);
        var churn = await _churn.PredictAsync(tenantId, cancellationToken: cancellationToken);
        var expansion = await _expansion.AnalyzeAsync(tenantId, cancellationToken);

        return new ExecutiveAiDashboardDto(
            await _audit.GetRecentAsync(tenantId, 30, cancellationToken),
            await _predictive.ForecastAsync(tenantId, cancellationToken),
            await _nba.GetForTenantAsync(tenantId, cancellationToken),
            new List<AgentRunResultDto>(),
            await _knowledge.GetInsightsAsync(tenantId, cancellationToken),
            pending,
            executedToday,
            churn.Count(c => c.ChurnProbability >= 60),
            expansion.Count(e => e.ReadinessLevel == "Ready"));
    }
}
