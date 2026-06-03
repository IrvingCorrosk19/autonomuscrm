namespace AutonomusCRM.Application.Autonomous;

public record OutcomeFabricStatusDto(
    Guid AuditId,
    bool HasDecision,
    bool HasExecution,
    bool HasBusinessOutcome,
    decimal? RevenueImpact,
    string LearningStatus);

public interface IOutcomeFabricService
{
    Task RecordExecutionAsync(Guid auditId, bool success, string detail, decimal? revenueImpact = null, CancellationToken cancellationToken = default);
    Task RecordBusinessOutcomeAsync(Guid auditId, bool succeeded, string detail, decimal revenueImpact, CancellationToken cancellationToken = default);
    Task<OutcomeFabricStatusDto?> GetStatusAsync(Guid auditId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiDecisionAudit>> GetIncompleteAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
}
