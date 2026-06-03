using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class AiDecisionAuditService : IAiDecisionAuditService
{
    private readonly IAiDecisionAuditRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public AiDecisionAuditService(IAiDecisionAuditRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> RecordAsync(
        AutonomousDecisionDto decision, Guid tenantId, string? agentName = null, CancellationToken cancellationToken = default)
    {
        OutcomeFabricService.EnrichDecisionEvidence(decision.Evidence, 0m, "medium");
        var audit = AiDecisionAudit.Create(
            tenantId, decision.DecisionType, decision.Action, decision.Score, decision.Reason,
            decision.Evidence, decision.CustomerId, decision.DealId, agentName: agentName);
        await _repository.AddAsync(audit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return audit.Id;
    }

    public async Task MarkExecutionOutcomeAsync(Guid auditId, string outcome, bool success, CancellationToken cancellationToken = default)
    {
        var audit = await _repository.GetByIdAsync(auditId, cancellationToken);
        if (audit == null) return;
        if (success) audit.MarkExecuted(outcome);
        else audit.MarkFailed(outcome);
        await _repository.UpdateAsync(audit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task MarkBusinessOutcomeAsync(
        Guid auditId, bool succeeded, string detail, CancellationToken cancellationToken = default)
    {
        var audit = await _repository.GetByIdAsync(auditId, cancellationToken);
        if (audit == null) return;
        audit.MarkBusinessOutcome(succeeded, detail);
        await _repository.UpdateAsync(audit, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<AutonomousDecisionDto>> GetRecentAsync(
        Guid tenantId, int take = 50, CancellationToken cancellationToken = default)
    {
        var list = await _repository.GetByTenantAsync(tenantId, take, cancellationToken);
        return list.Select(a => new AutonomousDecisionDto(
            a.Id, a.DecisionType, a.Action, a.DecisionScore, a.Reason, a.Evidence,
            a.CustomerId, a.DealId)).ToList();
    }
}
