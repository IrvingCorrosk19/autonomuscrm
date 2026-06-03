using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.SemanticMemory;

public class MemoryEmbedding : Entity
{
    public Guid TenantId { get; private set; }
    public string SourceType { get; private set; }
    public Guid SourceId { get; private set; }
    public string Text { get; private set; }
    public float[] EmbeddingVector { get; private set; }
    public string EmbeddingModel { get; private set; }
    public double RelevanceScore { get; private set; }
    public double ConfidenceScore { get; private set; }
    public int UsageCount { get; private set; }
    public DateTime? LastUsedAt { get; private set; }

    private MemoryEmbedding() : base()
    {
        SourceType = string.Empty;
        Text = string.Empty;
        EmbeddingVector = Array.Empty<float>();
        EmbeddingModel = "unknown";
    }

    public static MemoryEmbedding Create(
        Guid tenantId,
        string sourceType,
        Guid sourceId,
        string text,
        float[] vector,
        string model,
        double confidence = 0.7)
    {
        return new MemoryEmbedding
        {
            TenantId = tenantId,
            SourceType = sourceType,
            SourceId = sourceId,
            Text = text.Length > 8000 ? text[..8000] : text,
            EmbeddingVector = vector,
            EmbeddingModel = model,
            RelevanceScore = 0.5,
            ConfidenceScore = Math.Clamp(confidence, 0, 1),
            UsageCount = 0
        };
    }

    public void UpdateVector(float[] vector, string model)
    {
        EmbeddingVector = vector;
        EmbeddingModel = model;
        MarkAsUpdated();
    }

    public void RecordUsage(double relevanceBoost = 0.05)
    {
        UsageCount++;
        LastUsedAt = DateTime.UtcNow;
        RelevanceScore = Math.Min(1.0, RelevanceScore + relevanceBoost);
        MarkAsUpdated();
    }

    public void SetScores(double relevance, double confidence)
    {
        RelevanceScore = Math.Clamp(relevance, 0, 1);
        ConfidenceScore = Math.Clamp(confidence, 0, 1);
        MarkAsUpdated();
    }
}

public class CustomerMemoryProfile : Entity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string HistorySummary { get; private set; }
    public string RiskSummary { get; private set; }
    public string PreferencesSummary { get; private set; }
    public string SuccessfulDecisionsSummary { get; private set; }
    public string FailedDecisionsSummary { get; private set; }
    public string EffectiveChannelsSummary { get; private set; }
    public DateTime LastRefreshedAt { get; private set; }

    private CustomerMemoryProfile() : base()
    {
        HistorySummary = string.Empty;
        RiskSummary = string.Empty;
        PreferencesSummary = string.Empty;
        SuccessfulDecisionsSummary = string.Empty;
        FailedDecisionsSummary = string.Empty;
        EffectiveChannelsSummary = string.Empty;
    }

    public static CustomerMemoryProfile Create(Guid tenantId, Guid customerId) =>
        new()
        {
            TenantId = tenantId,
            CustomerId = customerId,
            LastRefreshedAt = DateTime.UtcNow
        };

    public void Refresh(
        string history,
        string risks,
        string preferences,
        string successful,
        string failed,
        string channels)
    {
        HistorySummary = history;
        RiskSummary = risks;
        PreferencesSummary = preferences;
        SuccessfulDecisionsSummary = successful;
        FailedDecisionsSummary = failed;
        EffectiveChannelsSummary = channels;
        LastRefreshedAt = DateTime.UtcNow;
        MarkAsUpdated();
    }
}
