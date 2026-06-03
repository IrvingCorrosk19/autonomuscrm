using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.Autonomous;

public class AiDecisionAudit : Entity
{
    public Guid TenantId { get; private set; }
    public string DecisionType { get; private set; }
    public string Action { get; private set; }
    public int DecisionScore { get; private set; }
    public string Reason { get; private set; }
    public Dictionary<string, object> Evidence { get; private set; }
    public string Status { get; private set; }
    public string? Outcome { get; private set; }
    /// <summary>True = business goal met (won deal, renewed, etc.); distinct from execution success.</summary>
    public bool? BusinessSucceeded { get; private set; }
    public DateTime? BusinessRecordedAt { get; private set; }
    public string? BusinessOutcomeDetail { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? DealId { get; private set; }
    public Guid? UserId { get; private set; }
    public string? AgentName { get; private set; }
    public DateTime? ExecutedAt { get; private set; }

    private AiDecisionAudit() : base()
    {
        DecisionType = string.Empty;
        Action = string.Empty;
        Reason = string.Empty;
        Evidence = new Dictionary<string, object>();
        Status = AutonomousConstants.AuditPending;
    }

    public static AiDecisionAudit Create(
        Guid tenantId,
        string decisionType,
        string action,
        int decisionScore,
        string reason,
        Dictionary<string, object>? evidence = null,
        Guid? customerId = null,
        Guid? dealId = null,
        Guid? userId = null,
        string? agentName = null)
    {
        return new AiDecisionAudit
        {
            TenantId = tenantId,
            DecisionType = decisionType,
            Action = action,
            DecisionScore = decisionScore,
            Reason = reason,
            Evidence = evidence ?? new Dictionary<string, object>(),
            CustomerId = customerId,
            DealId = dealId,
            UserId = userId,
            AgentName = agentName
        };
    }

    public void MarkExecuted(string? outcome = null)
    {
        Status = AutonomousConstants.AuditExecuted;
        Outcome = outcome ?? "Executed";
        ExecutedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }

    public void MarkFailed(string outcome)
    {
        Status = AutonomousConstants.AuditFailed;
        Outcome = outcome;
        MarkAsUpdated();
    }

    public void MarkBusinessOutcome(bool succeeded, string detail)
    {
        BusinessSucceeded = succeeded;
        BusinessOutcomeDetail = detail;
        BusinessRecordedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}

public class AutonomousPlaybookState : Entity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string PlaybookType { get; private set; }
    public string Status { get; private set; }
    public int CurrentStepIndex { get; private set; }
    public int TotalSteps { get; private set; }
    public DateTime? NextActionAt { get; private set; }
    public string? LastTaskType { get; private set; }

    private AutonomousPlaybookState() : base()
    {
        PlaybookType = string.Empty;
        Status = AutonomousConstants.PlaybookStateActive;
    }

    public static AutonomousPlaybookState Start(Guid tenantId, Guid customerId, string playbookType, int totalSteps)
    {
        return new AutonomousPlaybookState
        {
            TenantId = tenantId,
            CustomerId = customerId,
            PlaybookType = playbookType,
            TotalSteps = totalSteps,
            CurrentStepIndex = 0,
            NextActionAt = DateTime.UtcNow
        };
    }

    public void AdvanceStep(string taskType, int daysUntilNext = 1)
    {
        CurrentStepIndex++;
        LastTaskType = taskType;
        NextActionAt = DateTime.UtcNow.AddDays(daysUntilNext);
        if (CurrentStepIndex >= TotalSteps)
            Status = AutonomousConstants.PlaybookStateCompleted;
        MarkAsUpdated();
    }

    public void Escalate()
    {
        Status = AutonomousConstants.PlaybookStateEscalated;
        MarkAsUpdated();
    }
}

public class BusinessKnowledgeRecord : Entity
{
    public Guid TenantId { get; private set; }
    public string PatternKey { get; private set; }
    public string Outcome { get; private set; }
    public int Occurrences { get; private set; }
    public decimal SuccessRate { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private BusinessKnowledgeRecord() : base()
    {
        PatternKey = string.Empty;
        Outcome = AutonomousConstants.KnowledgeNeutral;
        Metadata = new Dictionary<string, object>();
    }

    public static BusinessKnowledgeRecord Create(Guid tenantId, string patternKey, string outcome)
    {
        return new BusinessKnowledgeRecord
        {
            TenantId = tenantId,
            PatternKey = patternKey,
            Outcome = outcome,
            Occurrences = 1,
            SuccessRate = outcome == AutonomousConstants.KnowledgeWin ? 100 : 0
        };
    }

    public void RecordOutcome(bool success)
    {
        Occurrences++;
        var wins = (int)Math.Round(SuccessRate / 100 * (Occurrences - 1));
        if (success) wins++;
        SuccessRate = Occurrences > 0 ? wins * 100m / Occurrences : 0;
        Outcome = SuccessRate >= 60 ? AutonomousConstants.KnowledgeWin
            : SuccessRate <= 40 ? AutonomousConstants.KnowledgeLoss
            : AutonomousConstants.KnowledgeNeutral;
        MarkAsUpdated();
    }
}

public class MlFeatureSnapshot : Entity
{
    public Guid TenantId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string DatasetType { get; private set; }
    public Dictionary<string, object> Features { get; private set; }
    public string? Label { get; private set; }
    public DateTime CapturedAt { get; private set; }

    private MlFeatureSnapshot() : base()
    {
        DatasetType = string.Empty;
        Features = new Dictionary<string, object>();
    }

    public static MlFeatureSnapshot Capture(
        Guid tenantId, string datasetType, Dictionary<string, object> features,
        string? label = null, Guid? customerId = null)
    {
        return new MlFeatureSnapshot
        {
            TenantId = tenantId,
            CustomerId = customerId,
            DatasetType = datasetType,
            Features = features,
            Label = label,
            CapturedAt = DateTime.UtcNow
        };
    }
}
