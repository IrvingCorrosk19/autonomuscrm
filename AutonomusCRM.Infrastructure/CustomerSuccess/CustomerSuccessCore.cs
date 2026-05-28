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

    public static int ScoreEngagement(Customer customer)
    {
        if (!customer.LastContactAt.HasValue)
            return 20;
        var days = (DateTime.UtcNow - customer.LastContactAt.Value).TotalDays;
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
        if (!onboarding.Any())
            return 50;
        var completed = onboarding.Count(t => t.Status == "Completed");
        return (int)Math.Round(completed * 100.0 / onboarding.Count);
    }

    public static int ScoreSupport(IEnumerable<WorkflowTask> openTasks)
    {
        var overdue = openTasks.Count(t => t.IsOverdue);
        var open = openTasks.Count();
        var penalty = overdue * 25 + open * 10;
        return Math.Max(0, 100 - penalty);
    }

    public static int ScoreRevenue(Customer customer, IEnumerable<Deal> wonDeals)
    {
        var ltv = customer.LifetimeValue ?? wonDeals.Sum(d => d.Amount);
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

    public static int ScoreRiskComponent(Customer customer)
        => 100 - Math.Clamp(customer.RiskScore ?? 50, 0, 100);

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
