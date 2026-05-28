using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class SelfLearningEngine : ISelfLearningEngine
{
    private readonly IAiDecisionAuditRepository _audits;
    private readonly IBusinessKnowledgeEngine _knowledge;
    private readonly INbaOutcomeRecordRepository _nbaOutcomes;
    private readonly IUnitOfWork _unitOfWork;

    public SelfLearningEngine(
        IAiDecisionAuditRepository audits,
        IBusinessKnowledgeEngine knowledge,
        INbaOutcomeRecordRepository nbaOutcomes,
        IUnitOfWork unitOfWork)
    {
        _audits = audits;
        _knowledge = knowledge;
        _nbaOutcomes = nbaOutcomes;
        _unitOfWork = unitOfWork;
    }

    public async Task<SelfLearningCycleResultDto> RunLearningCycleAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var audits = (await _audits.GetByTenantAsync(tenantId, 100, cancellationToken)).ToList();
        var outcomesProcessed = 0;
        var weightsUpdated = 0;

        foreach (var audit in audits.Where(a => a.Outcome != null && a.Status == AutonomousConstants.AuditExecuted))
        {
            var success = audit.Outcome is AutonomousConstants.KnowledgeWin or "Executed" or "Win";
            await _knowledge.RecordPatternOutcomeAsync(
                tenantId, $"{audit.DecisionType}:{audit.Action}", success, cancellationToken);
            outcomesProcessed++;
            weightsUpdated++;
        }

        var nba = (await _nbaOutcomes.GetRecentAsync(tenantId, 50, cancellationToken)).ToList();
        foreach (var record in nba)
        {
            await _knowledge.RecordPatternOutcomeAsync(
                tenantId, $"{record.EntityType}:{record.RecommendedAction}:{record.Channel}",
                record.Converted, cancellationToken);
        }

        return new SelfLearningCycleResultDto(
            outcomesProcessed + nba.Count,
            weightsUpdated,
            0,
            $"Processed {outcomesProcessed} decision outcomes; updated {weightsUpdated} knowledge weights.");
    }
}
