using AutonomusCRM.Domain.Common;

namespace AutonomusCRM.Application.Revenue;

public class SalesQuota : Entity
{
    public Guid TenantId { get; private set; }
    public Guid UserId { get; private set; }
    public string PeriodType { get; private set; }
    public DateTime PeriodStart { get; private set; }
    public DateTime PeriodEnd { get; private set; }
    public decimal TargetAmount { get; private set; }

    private SalesQuota() : base()
    {
        PeriodType = "Monthly";
    }

    public static SalesQuota Create(
        Guid tenantId,
        Guid userId,
        string periodType,
        DateTime periodStart,
        DateTime periodEnd,
        decimal targetAmount)
    {
        if (targetAmount <= 0)
            throw new ArgumentException("Target must be positive", nameof(targetAmount));

        return new SalesQuota
        {
            TenantId = tenantId,
            UserId = userId,
            PeriodType = periodType,
            PeriodStart = periodStart,
            PeriodEnd = periodEnd,
            TargetAmount = targetAmount
        };
    }

    public void UpdateTarget(decimal targetAmount, DateTime periodEnd)
    {
        if (targetAmount <= 0)
            throw new ArgumentException("Target must be positive", nameof(targetAmount));
        TargetAmount = targetAmount;
        PeriodEnd = periodEnd;
        MarkAsUpdated();
    }
}

public static class QuotaPeriodTypes
{
    public const string Monthly = "Monthly";
    public const string Quarterly = "Quarterly";
    public const string Yearly = "Yearly";
}
