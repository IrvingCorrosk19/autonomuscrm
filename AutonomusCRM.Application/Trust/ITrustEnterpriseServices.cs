namespace AutonomusCRM.Application.Trust;

public interface ITenantTrustPolicyService
{
    Task<int> GetApprovalThresholdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task SetApprovalThresholdAsync(Guid tenantId, int threshold, CancellationToken cancellationToken = default);
    Task<bool> RequiresHumanApprovalAsync(Guid tenantId, int decisionScore, CancellationToken cancellationToken = default);
}

public record TrustMetricsDto(
    int PendingApprovals,
    int ApprovedLast7Days,
    int RejectedLast7Days,
    int RolledBackLast7Days,
    double AvgPendingScore,
    int ApprovalThreshold);

public interface ITrustMetricsService
{
    Task<TrustMetricsDto> GetMetricsAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
