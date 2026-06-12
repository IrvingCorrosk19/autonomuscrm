using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

internal static class CustomerSuccessCore
{
    public static string ClassifyHealth(int score) => score switch
    {
        >= 70 => CustomerSuccessConstants.HealthHealthy,
        >= 40 => CustomerSuccessConstants.HealthWarning,
        _ => CustomerSuccessConstants.HealthCritical
    };

    public static int ScoreEngagement(Customer customer) => ScoreEngagement(customer.LastContactAt);

    public static int ScoreEngagement(DateTime? lastContactAt)
    {
        if (!lastContactAt.HasValue)
            return 20;
        var days = (DateTime.UtcNow - lastContactAt.Value).TotalDays;
        return days switch
        {
            <= 7 => 100,
            <= 30 => 80,
            <= 60 => 50,
            <= 90 => 30,
            _ => 10
        };
    }

    public static int ScoreAdoption(IEnumerable<WorkflowTask> tasks)
    {
        var onboarding = tasks.Where(t => t.TaskType != null && t.TaskType.StartsWith("Onboarding_", StringComparison.Ordinal)).ToList();
        return ScoreAdoption(onboarding.Count, onboarding.Count(t => t.Status == "Completed"));
    }

    public static int ScoreAdoption(int onboardingTotal, int onboardingCompleted)
    {
        if (onboardingTotal == 0)
            return 50;
        return (int)Math.Round(onboardingCompleted * 100.0 / onboardingTotal);
    }

    public static int ScoreSupport(IEnumerable<WorkflowTask> openTasks)
    {
        var open = openTasks.ToList();
        return ScoreSupport(open.Count, open.Count(t => t.IsOverdue));
    }

    public static int ScoreSupport(int openTaskCount, int overdueOpenCount)
    {
        var penalty = overdueOpenCount * 25 + openTaskCount * 10;
        return Math.Max(0, 100 - penalty);
    }

    public static int ScoreRevenue(Customer customer, IEnumerable<Deal> wonDeals)
        => ScoreRevenue(customer.LifetimeValue, wonDeals.Sum(d => d.Amount));

    public static int ScoreRevenue(decimal? lifetimeValue, decimal wonAmountSum)
    {
        var ltv = lifetimeValue ?? wonAmountSum;
        if (ltv <= 0)
            return 30;
        if (ltv >= 100_000)
            return 100;
        if (ltv >= 50_000)
            return 85;
        if (ltv >= 10_000)
            return 70;
        return 50;
    }

    public static int ScoreRiskComponent(Customer customer) => ScoreRiskComponent(customer.RiskScore);

    public static int ScoreRiskComponent(int? riskScore)
        => 100 - Math.Clamp(riskScore ?? 50, 0, 100);

    public static int CompositeHealth(int adoption, int engagement, int support, int revenue, int riskComponent)
        => (int)Math.Round(adoption * 0.2 + engagement * 0.25 + support * 0.15 + revenue * 0.2 + riskComponent * 0.2);

    public static IEnumerable<WorkflowTask> TasksForCustomer(
        Guid customerId,
        IEnumerable<WorkflowTask> allTasks,
        IEnumerable<Deal> deals)
    {
        var dealIds = deals.Where(d => d.CustomerId == customerId).Select(d => d.Id).ToHashSet();
        return allTasks.Where(t =>
            (t.RelatedEntityType == "Customer" && t.RelatedEntityId == customerId)
            || (t.RelatedEntityType == "Deal" && t.RelatedEntityId.HasValue && dealIds.Contains(t.RelatedEntityId.Value)));
    }
}
