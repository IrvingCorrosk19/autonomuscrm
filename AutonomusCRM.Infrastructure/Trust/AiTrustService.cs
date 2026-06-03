using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Trust;
using AutonomusCRM.Infrastructure.Autonomous;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Trust;

public sealed class AiApprovalRepository : IAiApprovalRepository
{
    private readonly ApplicationDbContext _db;

    public AiApprovalRepository(ApplicationDbContext db) => _db = db;

    public async Task AddAsync(AiApprovalRequest request, CancellationToken cancellationToken = default)
    {
        await _db.AiApprovalRequests.AddAsync(request, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<AiApprovalRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _db.AiApprovalRequests.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AiApprovalRequest>> GetPendingAsync(Guid tenantId, int take, CancellationToken cancellationToken = default)
        => await _db.AiApprovalRequests
            .Where(x => x.TenantId == tenantId && x.Status == "pending")
            .OrderByDescending(x => x.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task UpdateAsync(AiApprovalRequest request, CancellationToken cancellationToken = default)
    {
        _db.AiApprovalRequests.Update(request);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class AiTrustService : IAiTrustService
{
    private readonly IAiApprovalRepository _approvals;
    private readonly IAiDecisionAuditRepository _auditRepo;
    private readonly IAiDecisionAuditService _audits;
    private readonly IAutonomousDecisionExecutor _executor;
    private readonly ApplicationDbContext _db;
    private readonly ILogger<AiTrustService> _logger;

    public AiTrustService(
        IAiApprovalRepository approvals,
        IAiDecisionAuditRepository auditRepo,
        IAiDecisionAuditService audits,
        IAutonomousDecisionExecutor executor,
        ApplicationDbContext db,
        ILogger<AiTrustService> logger)
    {
        _approvals = approvals;
        _auditRepo = auditRepo;
        _audits = audits;
        _executor = executor;
        _db = db;
        _logger = logger;
    }

    public async Task<Guid> QueueForApprovalAsync(
        Guid tenantId, Guid auditId, string decisionType, string action, string explanation,
        CancellationToken cancellationToken = default)
    {
        var request = AiApprovalRequest.Create(tenantId, auditId, decisionType, action, explanation);
        await _approvals.AddAsync(request, cancellationToken);
        return request.Id;
    }

    public async Task<IReadOnlyList<ApprovalInboxItemDto>> GetInboxAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var items = await _approvals.GetPendingAsync(tenantId, 50, cancellationToken);
        var result = new List<ApprovalInboxItemDto>();

        foreach (var item in items)
        {
            var audit = await _auditRepo.GetByIdAsync(item.AuditId, cancellationToken);
            string? customerName = null, dealTitle = null;
            if (audit?.CustomerId is Guid cid)
                customerName = (await _db.Customers.FindAsync(new object[] { cid }, cancellationToken))?.Name;
            if (audit?.DealId is Guid did)
                dealTitle = (await _db.Deals.FindAsync(new object[] { did }, cancellationToken))?.Title;

            var score = audit?.DecisionScore ?? 0;
            var risk = score >= 85 ? "Alto" : score >= 70 ? "Medio" : "Bajo";

            result.Add(new ApprovalInboxItemDto(
                item.Id, item.AuditId, item.DecisionType, item.RecommendedAction, item.Explanation,
                item.Status, item.CreatedAt, score, risk, audit?.CustomerId, customerName, audit?.DealId, dealTitle));
        }

        return result;
    }

    public async Task ApproveAsync(Guid tenantId, Guid approvalId, Guid userId, bool executeDecision, CancellationToken cancellationToken = default)
    {
        var item = await _approvals.GetByIdAsync(approvalId, cancellationToken)
            ?? throw new InvalidOperationException("Approval not found");
        if (item.TenantId != tenantId) throw new UnauthorizedAccessException();
        item.Approve(userId);
        await _approvals.UpdateAsync(item, cancellationToken);

        if (executeDecision)
        {
            var audit = await _auditRepo.GetByIdAsync(item.AuditId, cancellationToken);
            if (audit != null)
                await _executor.ExecuteApprovedAuditAsync(tenantId, audit, cancellationToken);
        }

        _logger.LogInformation("Approval {Id} approved by {UserId}", approvalId, userId);
    }

    public async Task RejectAsync(Guid tenantId, Guid approvalId, Guid userId, string? note, CancellationToken cancellationToken = default)
    {
        var item = await _approvals.GetByIdAsync(approvalId, cancellationToken)
            ?? throw new InvalidOperationException("Approval not found");
        if (item.TenantId != tenantId) throw new UnauthorizedAccessException();
        item.Reject(userId, note);
        await _approvals.UpdateAsync(item, cancellationToken);
        await _audits.MarkExecutionOutcomeAsync(item.AuditId, note ?? "Rejected", false, cancellationToken);
    }

    public async Task RollbackAsync(Guid tenantId, Guid approvalId, Guid userId, string note, CancellationToken cancellationToken = default)
    {
        var item = await _approvals.GetByIdAsync(approvalId, cancellationToken)
            ?? throw new InvalidOperationException("Approval not found");
        if (item.TenantId != tenantId) throw new UnauthorizedAccessException();
        item.Rollback(userId, note);
        await _approvals.UpdateAsync(item, cancellationToken);
        await _audits.MarkBusinessOutcomeAsync(item.AuditId, false, $"Rollback: {note}", cancellationToken);
    }
}
