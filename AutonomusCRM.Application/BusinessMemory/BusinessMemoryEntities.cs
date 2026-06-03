using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.BusinessMemory;

/// <summary>Raíz de un episodio de memoria empresarial (qué pasó, cuándo, por qué).</summary>
public class BusinessMemoryRoot : Entity
{
    public Guid TenantId { get; private set; }
    public string SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public string EpisodeKey { get; private set; }
    public string Title { get; private set; }
    public string Summary { get; private set; }
    public string MemoryType { get; private set; }
    public int Importance { get; private set; }
    public string SourceChannel { get; private set; }
    public List<string> Tags { get; private set; }

    private BusinessMemoryRoot() : base()
    {
        SubjectType = string.Empty;
        EpisodeKey = string.Empty;
        Title = string.Empty;
        Summary = string.Empty;
        MemoryType = BusinessMemoryConstants.MemoryTypeEpisode;
        SourceChannel = "system";
        Tags = new List<string>();
    }

    public static BusinessMemoryRoot CreateEpisode(
        Guid tenantId,
        string subjectType,
        Guid subjectId,
        string episodeKey,
        string title,
        string summary,
        int importance = 5,
        string sourceChannel = "domain_event",
        IEnumerable<string>? tags = null)
    {
        return new BusinessMemoryRoot
        {
            TenantId = tenantId,
            SubjectType = subjectType,
            SubjectId = subjectId,
            EpisodeKey = episodeKey,
            Title = title,
            Summary = summary,
            MemoryType = BusinessMemoryConstants.MemoryTypeEpisode,
            Importance = Math.Clamp(importance, 1, 10),
            SourceChannel = sourceChannel,
            Tags = tags?.ToList() ?? new List<string>()
        };
    }

    public void EnrichSummary(string additional)
    {
        if (string.IsNullOrWhiteSpace(additional)) return;
        Summary = string.IsNullOrWhiteSpace(Summary) ? additional : $"{Summary} | {additional}";
        MarkAsUpdated();
    }
}

public class BusinessMemoryEvent : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? DomainEventId { get; private set; }
    public string EventType { get; private set; }
    public Dictionary<string, object> Payload { get; private set; }
    public DateTime OccurredAt { get; private set; }
    public string? ActorType { get; private set; }
    public Guid? ActorId { get; private set; }
    public string Narrative { get; private set; }

    private BusinessMemoryEvent() : base()
    {
        EventType = string.Empty;
        Payload = new Dictionary<string, object>();
        Narrative = string.Empty;
    }

    public static BusinessMemoryEvent FromDomain(
        Guid memoryId, Guid tenantId, string eventType, string narrative,
        Guid? domainEventId, DateTime occurredAt, Dictionary<string, object>? payload = null,
        string? actorType = null, Guid? actorId = null)
    {
        return new BusinessMemoryEvent
        {
            MemoryId = memoryId,
            TenantId = tenantId,
            DomainEventId = domainEventId,
            EventType = eventType,
            Narrative = narrative,
            OccurredAt = occurredAt,
            Payload = payload ?? new Dictionary<string, object>(),
            ActorType = actorType,
            ActorId = actorId
        };
    }
}

public class BusinessMemoryFact : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid TenantId { get; private set; }
    public string FactKey { get; private set; }
    public string FactValue { get; private set; }
    public double Confidence { get; private set; }

    private BusinessMemoryFact() : base()
    {
        FactKey = string.Empty;
        FactValue = string.Empty;
    }

    public static BusinessMemoryFact Create(Guid memoryId, Guid tenantId, string key, string value, double confidence = 1.0)
    {
        return new BusinessMemoryFact
        {
            MemoryId = memoryId,
            TenantId = tenantId,
            FactKey = key,
            FactValue = value,
            Confidence = Math.Clamp(confidence, 0, 1)
        };
    }
}

public class BusinessMemoryOutcome : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid TenantId { get; private set; }
    public string OutcomeCategory { get; private set; }
    public bool Succeeded { get; private set; }
    public decimal RevenueDelta { get; private set; }
    public int CustomerImpactScore { get; private set; }
    public int TrustImpactScore { get; private set; }
    public string Narrative { get; private set; }

    private BusinessMemoryOutcome() : base()
    {
        OutcomeCategory = string.Empty;
        Narrative = string.Empty;
    }

    public static BusinessMemoryOutcome Record(
        Guid memoryId, Guid tenantId, string category, bool succeeded, string narrative,
        decimal revenueDelta = 0, int customerImpact = 0, int trustImpact = 0)
    {
        return new BusinessMemoryOutcome
        {
            MemoryId = memoryId,
            TenantId = tenantId,
            OutcomeCategory = category,
            Succeeded = succeeded,
            Narrative = narrative,
            RevenueDelta = revenueDelta,
            CustomerImpactScore = customerImpact,
            TrustImpactScore = trustImpact
        };
    }
}

public class BusinessMemoryDecision : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? AiDecisionAuditId { get; private set; }
    public string DecisionType { get; private set; }
    public string Action { get; private set; }
    public string Reason { get; private set; }
    public int Score { get; private set; }
    public bool? WasSuccessful { get; private set; }
    public Dictionary<string, object> Context { get; private set; }

    private BusinessMemoryDecision() : base()
    {
        DecisionType = string.Empty;
        Action = string.Empty;
        Reason = string.Empty;
        Context = new Dictionary<string, object>();
    }

    public static BusinessMemoryDecision FromAudit(
        Guid memoryId, Guid tenantId, Guid auditId, string decisionType, string action,
        string reason, int score, Dictionary<string, object>? context = null)
    {
        return new BusinessMemoryDecision
        {
            MemoryId = memoryId,
            TenantId = tenantId,
            AiDecisionAuditId = auditId,
            DecisionType = decisionType,
            Action = action,
            Reason = reason,
            Score = score,
            Context = context ?? new Dictionary<string, object>()
        };
    }

    public void SetOutcome(bool success)
    {
        WasSuccessful = success;
        MarkAsUpdated();
    }
}

public class BusinessMemoryRelationship : Entity
{
    public Guid TenantId { get; private set; }
    public Guid? MemoryId { get; private set; }
    public string FromType { get; private set; }
    public Guid FromId { get; private set; }
    public string ToType { get; private set; }
    public Guid ToId { get; private set; }
    public string RelationType { get; private set; }
    public double Weight { get; private set; }
    public Dictionary<string, object> Metadata { get; private set; }

    private BusinessMemoryRelationship() : base()
    {
        FromType = string.Empty;
        ToType = string.Empty;
        RelationType = string.Empty;
        Metadata = new Dictionary<string, object>();
    }

    public static BusinessMemoryRelationship Link(
        Guid tenantId, string fromType, Guid fromId, string toType, Guid toId,
        string relationType, Guid? memoryId = null, double weight = 1.0)
    {
        return new BusinessMemoryRelationship
        {
            TenantId = tenantId,
            MemoryId = memoryId,
            FromType = fromType,
            FromId = fromId,
            ToType = toType,
            ToId = toId,
            RelationType = relationType,
            Weight = weight
        };
    }
}

public class BusinessMemoryInsight : Entity
{
    public Guid TenantId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? MemoryId { get; private set; }
    public string InsightType { get; private set; }
    public string Content { get; private set; }
    public double Confidence { get; private set; }

    private BusinessMemoryInsight() : base()
    {
        InsightType = string.Empty;
        Content = string.Empty;
    }

    public static BusinessMemoryInsight Create(
        Guid tenantId, string insightType, string content, double confidence,
        Guid? customerId = null, Guid? memoryId = null)
    {
        return new BusinessMemoryInsight
        {
            TenantId = tenantId,
            CustomerId = customerId,
            MemoryId = memoryId,
            InsightType = insightType,
            Content = content,
            Confidence = Math.Clamp(confidence, 0, 1)
        };
    }
}

public class BusinessMemoryObservation : Entity
{
    public Guid TenantId { get; private set; }
    public Guid? MemoryId { get; private set; }
    public string Channel { get; private set; }
    public string Content { get; private set; }
    public string SubjectType { get; private set; }
    public Guid SubjectId { get; private set; }
    public DateTime ObservedAt { get; private set; }

    private BusinessMemoryObservation() : base()
    {
        Channel = string.Empty;
        Content = string.Empty;
        SubjectType = string.Empty;
    }

    public static BusinessMemoryObservation Record(
        Guid tenantId, string channel, string content, string subjectType, Guid subjectId,
        Guid? memoryId = null)
    {
        return new BusinessMemoryObservation
        {
            TenantId = tenantId,
            MemoryId = memoryId,
            Channel = channel,
            Content = content,
            SubjectType = subjectType,
            SubjectId = subjectId,
            ObservedAt = DateTime.UtcNow
        };
    }
}

public class BusinessMemoryLearning : Entity
{
    public Guid TenantId { get; private set; }
    public string StrategyKey { get; private set; }
    public string ActionTaken { get; private set; }
    public Dictionary<string, object> ContextPattern { get; private set; }
    public int SuccessCount { get; private set; }
    public int FailureCount { get; private set; }
    public decimal SuccessRate { get; private set; }
    public DateTime? LastAppliedAt { get; private set; }
    public string? LastOutcome { get; private set; }

    private BusinessMemoryLearning() : base()
    {
        StrategyKey = string.Empty;
        ActionTaken = string.Empty;
        ContextPattern = new Dictionary<string, object>();
    }

    public static BusinessMemoryLearning Start(
        Guid tenantId, string strategyKey, string action, Dictionary<string, object>? context = null)
    {
        return new BusinessMemoryLearning
        {
            TenantId = tenantId,
            StrategyKey = strategyKey,
            ActionTaken = action,
            ContextPattern = context ?? new Dictionary<string, object>(),
            SuccessCount = 0,
            FailureCount = 0,
            SuccessRate = 0
        };
    }

    public void ApplyOutcome(bool success, string outcomeLabel)
    {
        if (success) SuccessCount++;
        else FailureCount++;
        var total = SuccessCount + FailureCount;
        SuccessRate = total > 0 ? (decimal)SuccessCount * 100m / total : 0;
        LastAppliedAt = DateTime.UtcNow;
        LastOutcome = outcomeLabel;
        MarkAsUpdated();
    }
}

public class BusinessMemoryContext : Entity
{
    public Guid MemoryId { get; private set; }
    public Guid TenantId { get; private set; }
    public string ContextLayer { get; private set; }
    public Dictionary<string, object> Snapshot { get; private set; }
    public DateTime CapturedAt { get; private set; }

    private BusinessMemoryContext() : base()
    {
        ContextLayer = string.Empty;
        Snapshot = new Dictionary<string, object>();
    }

    public static BusinessMemoryContext Capture(
        Guid memoryId, Guid tenantId, string layer, Dictionary<string, object> snapshot)
    {
        return new BusinessMemoryContext
        {
            MemoryId = memoryId,
            TenantId = tenantId,
            ContextLayer = layer,
            Snapshot = snapshot,
            CapturedAt = DateTime.UtcNow
        };
    }
}
