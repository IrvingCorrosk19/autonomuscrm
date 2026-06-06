using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.Intelligence;

public class ProductUsageEvent : Entity
{
    public Guid TenantId { get; private set; }
    public Guid? UserId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public string Module { get; private set; }
    public string EventType { get; private set; }
    public int DurationMinutes { get; private set; }
    public string? SessionId { get; private set; }
    public string? Industry { get; private set; }
    public DateTime RecordedAt { get; private set; }

    private ProductUsageEvent() : base()
    {
        Module = string.Empty;
        EventType = string.Empty;
    }

    public static ProductUsageEvent Create(
        Guid tenantId,
        string module,
        string eventType,
        Guid? userId = null,
        Guid? customerId = null,
        int durationMinutes = 0,
        string? sessionId = null,
        string? industry = null)
    {
        return new ProductUsageEvent
        {
            TenantId = tenantId,
            UserId = userId,
            CustomerId = customerId,
            Module = module,
            EventType = eventType,
            DurationMinutes = durationMinutes,
            SessionId = sessionId,
            Industry = industry,
            RecordedAt = DateTime.UtcNow
        };
    }
}

public class CustomerFeedback : Entity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public string FeedbackType { get; private set; }
    public int Score { get; private set; }
    public string? Comment { get; private set; }
    public string? Segment { get; private set; }
    public Guid? SubmittedByUserId { get; private set; }
    public DateTime SubmittedAt { get; private set; }

    private CustomerFeedback() : base()
    {
        FeedbackType = string.Empty;
    }

    public static CustomerFeedback Create(
        Guid tenantId,
        Guid customerId,
        string feedbackType,
        int score,
        string? comment = null,
        string? segment = null,
        Guid? submittedByUserId = null)
    {
        if (feedbackType == IntelligenceConstants.FeedbackNps && (score < 0 || score > 10))
            throw new ArgumentException("Validation_NpsScore_Range", nameof(score));
        if (feedbackType == IntelligenceConstants.FeedbackCsat && (score < 1 || score > 5))
            throw new ArgumentException("Validation_CsatScore_Range", nameof(score));

        return new CustomerFeedback
        {
            TenantId = tenantId,
            CustomerId = customerId,
            FeedbackType = feedbackType,
            Score = score,
            Comment = comment,
            Segment = segment,
            SubmittedByUserId = submittedByUserId,
            SubmittedAt = DateTime.UtcNow
        };
    }
}

public class CustomerAnalyticsSnapshot : Entity
{
    public Guid TenantId { get; private set; }
    public Guid CustomerId { get; private set; }
    public DateTime SnapshotDate { get; private set; }
    public int HealthScore { get; private set; }
    public int ChurnRiskScore { get; private set; }
    public int? NpsScore { get; private set; }
    public decimal? CsatScore { get; private set; }
    public decimal RevenueAmount { get; private set; }
    public int ExpansionScore { get; private set; }
    public string Segment { get; private set; }
    public int EngagementScore { get; private set; }
    public int AdoptionScore { get; private set; }
    public int ActiveUsers { get; private set; }

    private CustomerAnalyticsSnapshot() : base()
    {
        Segment = IntelligenceConstants.SegmentStable;
    }

    public static CustomerAnalyticsSnapshot Create(
        Guid tenantId,
        Guid customerId,
        DateTime snapshotDate,
        int healthScore,
        int churnRiskScore,
        int? npsScore,
        decimal? csatScore,
        decimal revenueAmount,
        int expansionScore,
        string segment,
        int engagementScore,
        int adoptionScore,
        int activeUsers)
    {
        return new CustomerAnalyticsSnapshot
        {
            TenantId = tenantId,
            CustomerId = customerId,
            SnapshotDate = snapshotDate.Date,
            HealthScore = healthScore,
            ChurnRiskScore = churnRiskScore,
            NpsScore = npsScore,
            CsatScore = csatScore,
            RevenueAmount = revenueAmount,
            ExpansionScore = expansionScore,
            Segment = segment,
            EngagementScore = engagementScore,
            AdoptionScore = adoptionScore,
            ActiveUsers = activeUsers
        };
    }
}
