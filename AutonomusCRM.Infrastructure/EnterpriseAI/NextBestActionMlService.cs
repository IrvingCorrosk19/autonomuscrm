using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Infrastructure.EnterpriseAI.MlMath;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class NextBestActionMlService : INextBestActionMlScorer
{
    private readonly INbaOutcomeRecordRepository _outcomes;
    private readonly IBusinessKnowledgeEngine _knowledge;
    private readonly IMlModelVersionRepository _models;
    private readonly IUnitOfWork _unitOfWork;

    public NextBestActionMlService(
        INbaOutcomeRecordRepository outcomes,
        IBusinessKnowledgeEngine knowledge,
        IMlModelVersionRepository models,
        IUnitOfWork unitOfWork)
    {
        _outcomes = outcomes;
        _knowledge = knowledge;
        _models = models;
        _unitOfWork = unitOfWork;
    }

    public int ScoreAction(string action, string channel, string entityType, Guid tenantId)
    {
        var patternKey = $"{entityType}:{action}:{channel}";
        var preferred = _knowledge.ResolvePreferredAction(action, tenantId);
        var baseScore = preferred == action ? 15 : 0;

        var recent = _outcomes.GetRecentAsync(tenantId, 200).GetAwaiter().GetResult();
        var matching = recent.Where(r => r.RecommendedAction == action && r.Channel == channel).ToList();
        if (matching.Count > 0)
        {
            var convRate = matching.Count(m => m.Converted) * 100.0 / matching.Count;
            baseScore += (int)Math.Round(convRate * 0.4);
        }

        var active = _models.GetActiveAsync(tenantId, EnterpriseAiConstants.ModelNba).GetAwaiter().GetResult();
        if (active?.Metrics.TryGetValue("f1", out var f1) == true)
            baseScore += (int)Math.Round(MlFeatureExtractor.ToNumeric(f1) * 20);

        return Math.Clamp(baseScore, 0, 40);
    }

    public async Task RecordOutcomeAsync(
        Guid tenantId, string entityType, Guid entityId, string action, string channel,
        bool converted, decimal impact = 0, CancellationToken cancellationToken = default)
    {
        await _outcomes.AddAsync(NbaOutcomeRecord.FromAction(
            tenantId, entityType, entityId, action, channel, converted, impact), cancellationToken);
        await _knowledge.RecordPatternOutcomeAsync(tenantId, $"{entityType}:{action}:{channel}", converted, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }
}
