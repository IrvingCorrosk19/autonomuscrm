namespace AutonomusCRM.Application.Trust;

public class AiApprovalRequest
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid AuditId { get; private set; }
    public string DecisionType { get; private set; } = string.Empty;
    public string RecommendedAction { get; private set; } = string.Empty;
    public string Explanation { get; private set; } = string.Empty;
    public string Status { get; private set; } = "pending";
    public Guid? ReviewedByUserId { get; private set; }
    public DateTime? ReviewedAt { get; private set; }
    public string? ReviewNote { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private AiApprovalRequest() { }

    public static AiApprovalRequest Create(
        Guid tenantId, Guid auditId, string decisionType, string action, string explanation)
    {
        return new AiApprovalRequest
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            AuditId = auditId,
            DecisionType = decisionType,
            RecommendedAction = action,
            Explanation = explanation,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Approve(Guid userId, string? note = null)
    {
        Status = "approved";
        ReviewedByUserId = userId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNote = note;
    }

    public void Reject(Guid userId, string? note = null)
    {
        Status = "rejected";
        ReviewedByUserId = userId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNote = note;
    }

    public void Rollback(Guid userId, string note)
    {
        Status = "rolled_back";
        ReviewedByUserId = userId;
        ReviewedAt = DateTime.UtcNow;
        ReviewNote = note;
    }
}

public record ApprovalInboxItemDto(
    Guid Id,
    Guid AuditId,
    string DecisionType,
    string RecommendedAction,
    string Explanation,
    string Status,
    DateTime CreatedAt,
    int DecisionScore,
    string RiskLevel,
    Guid? CustomerId,
    string? CustomerName,
    Guid? DealId,
    string? DealTitle);
