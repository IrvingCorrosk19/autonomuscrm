using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class BusinessKnowledgeEngine : IBusinessKnowledgeEngine
{
    private readonly IBusinessKnowledgeRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public BusinessKnowledgeEngine(IBusinessKnowledgeRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task RecordPatternOutcomeAsync(
        Guid tenantId, string patternKey, bool success, CancellationToken cancellationToken = default)
    {
        var record = await _repository.GetByPatternAsync(tenantId, patternKey, cancellationToken);
        if (record == null)
        {
            record = BusinessKnowledgeRecord.Create(tenantId, patternKey,
                success ? AutonomousConstants.KnowledgeWin : AutonomousConstants.KnowledgeLoss);
            await _repository.AddAsync(record, cancellationToken);
        }
        else
        {
            record.RecordOutcome(success);
            await _repository.UpdateAsync(record, cancellationToken);
        }
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<KnowledgeInsightDto>> GetInsightsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var all = (await _repository.GetAllAsync(cancellationToken))
            .Where(k => k.TenantId == tenantId)
            .OrderByDescending(k => k.SuccessRate)
            .Take(20)
            .Select(k => new KnowledgeInsightDto(
                k.PatternKey, k.SuccessRate, k.Occurrences,
                k.SuccessRate >= 60 ? $"Repetir patrón {k.PatternKey}" : $"Revisar patrón {k.PatternKey}"))
            .ToList();
        return all;
    }

    public string ResolvePreferredAction(string decisionType, Guid tenantId)
    {
        return decisionType switch
        {
            AutonomousConstants.DecisionRescue => "ExecuteRescuePlaybook",
            AutonomousConstants.DecisionRenewal => "ExecuteRenewalPlaybook",
            AutonomousConstants.DecisionExpansion => "ExecuteExpansionPlaybook",
            AutonomousConstants.DecisionReEngagement => "ReEngagementCampaign",
            AutonomousConstants.DecisionUpsell => "ProposeUpsell",
            AutonomousConstants.DecisionEscalation => "EscalateToExecutive",
            _ => "Monitor"
        };
    }
}
