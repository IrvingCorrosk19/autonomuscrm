namespace AutonomusCRM.Application.Trust;

public interface IAiApprovalRepository
{
    Task AddAsync(AiApprovalRequest request, CancellationToken cancellationToken = default);
    Task<AiApprovalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AiApprovalRequest>> GetPendingAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
    Task UpdateAsync(AiApprovalRequest request, CancellationToken cancellationToken = default);
}

public interface IAiTrustService
{
    Task<Guid> QueueForApprovalAsync(Guid tenantId, Guid auditId, string decisionType, string action, string explanation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ApprovalInboxItemDto>> GetInboxAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task ApproveAsync(Guid tenantId, Guid approvalId, Guid userId, bool executeDecision, CancellationToken cancellationToken = default);
    Task RejectAsync(Guid tenantId, Guid approvalId, Guid userId, string? note, CancellationToken cancellationToken = default);
    Task RollbackAsync(Guid tenantId, Guid approvalId, Guid userId, string note, CancellationToken cancellationToken = default);
}
