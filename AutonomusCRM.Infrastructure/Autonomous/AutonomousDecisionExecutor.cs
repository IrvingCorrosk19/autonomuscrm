using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.CustomerSuccess;

namespace AutonomusCRM.Infrastructure.Autonomous;

public interface IAutonomousDecisionExecutor
{
    Task ExecuteApprovedAuditAsync(Guid tenantId, AiDecisionAudit audit, CancellationToken cancellationToken = default);
}

public sealed class AutonomousDecisionExecutor : IAutonomousDecisionExecutor
{
    private readonly IAutonomousPlaybookEngine _playbook;
    private readonly IAutonomousCommunicationsEngine _communications;
    private readonly IAiDecisionAuditService _audits;
    private readonly IOutcomeFabricService _outcomeFabric;

    public AutonomousDecisionExecutor(
        IAutonomousPlaybookEngine playbook,
        IAutonomousCommunicationsEngine communications,
        IAiDecisionAuditService audits,
        IOutcomeFabricService outcomeFabric)
    {
        _playbook = playbook;
        _communications = communications;
        _audits = audits;
        _outcomeFabric = outcomeFabric;
    }

    public async Task ExecuteApprovedAuditAsync(Guid tenantId, AiDecisionAudit audit, CancellationToken cancellationToken = default)
    {
        var decision = new AutonomousDecisionDto(
            audit.Id,
            audit.DecisionType,
            audit.Action,
            audit.DecisionScore,
            audit.Reason,
            audit.Evidence,
            audit.CustomerId,
            audit.DealId);

        if (audit.CustomerId.HasValue)
        {
            var playbook = audit.DecisionType switch
            {
                AutonomousConstants.DecisionRescue => CustomerSuccessConstants.PlaybookRescue,
                AutonomousConstants.DecisionRenewal => CustomerSuccessConstants.PlaybookRenewal,
                AutonomousConstants.DecisionExpansion or AutonomousConstants.DecisionUpsell => CustomerSuccessConstants.PlaybookExpansion,
                AutonomousConstants.DecisionReEngagement => CustomerSuccessConstants.PlaybookReEngagement,
                _ => CustomerSuccessConstants.PlaybookAdoption
            };
            await _playbook.StartOrAdvanceAsync(tenantId, audit.CustomerId.Value, playbook, cancellationToken);
        }

        await _communications.ExecuteForDecisionAsync(tenantId, decision, cancellationToken);
        await _audits.MarkExecutionOutcomeAsync(audit.Id, audit.Action, true, cancellationToken);
        await _outcomeFabric.RecordExecutionAsync(audit.Id, true, audit.Action, null, cancellationToken);
    }
}
