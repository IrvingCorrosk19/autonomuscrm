namespace AutonomusCRM.Application.Trust;

public record TrustSlaAlertDto(
    Guid ApprovalId,
    Guid AuditId,
    string DecisionType,
    int HoursPending,
    string Severity);

public interface ITrustSlaService
{
    Task<IReadOnlyList<TrustSlaAlertDto>> GetOverdueApprovalsAsync(Guid tenantId, int slaHours = 24, CancellationToken cancellationToken = default);
}
