using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;

namespace AutonomusCRM.Infrastructure.EnterpriseAI;

public class AiGovernanceService : IAiGovernanceService
{
    private readonly IMlModelVersionRepository _models;
    private readonly IAiDecisionAuditRepository _audits;
    private readonly IMlDriftReportRepository _drift;
    private readonly IModelRegistryService _registry;

    public AiGovernanceService(
        IMlModelVersionRepository models,
        IAiDecisionAuditRepository audits,
        IMlDriftReportRepository drift,
        IModelRegistryService registry)
    {
        _models = models;
        _audits = audits;
        _drift = drift;
        _registry = registry;
    }

    public async Task<AiGovernanceReportDto> GetGovernanceReportAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var modelAudits = new List<ModelAuditEntryDto>();
        var total = 0;
        var active = 0;

        foreach (var mt in EnterpriseAiConstants.AllModelTypes)
        {
            var versions = (await _models.GetByTypeAsync(tenantId, mt, cancellationToken)).ToList();
            total += versions.Count;
            var act = versions.FirstOrDefault(v => v.Status == EnterpriseAiConstants.ModelStatusActive);
            if (act != null)
            {
                active++;
                modelAudits.Add(new ModelAuditEntryDto(
                    act.ModelType, act.VersionTag, act.TrainedAt, act.Metrics, act.Status));
            }
        }

        var recentAudits = (await _audits.GetByTenantAsync(tenantId, 30, cancellationToken)).ToList();
        var explanations = recentAudits.Select(a =>
        {
            a.Evidence.TryGetValue("model_version", out var mv);
            return new DecisionExplainabilityDto(
                a.Id, a.DecisionType, a.Action, a.DecisionScore, a.Reason,
                a.Evidence, mv?.ToString());
        }).ToList();

        var driftAlerts = (await _drift.GetRecentAsync(tenantId, 10, cancellationToken))
            .Count(d => d.AlertTriggered);

        return new AiGovernanceReportDto(modelAudits, explanations, total, active, driftAlerts);
    }
}
