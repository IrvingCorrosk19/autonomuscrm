namespace AutonomusCRM.Application.Billing;

public record PlanLimitsProfile(
    int MaxUsers,
    int MaxCustomers,
    int MaxLeads,
    int MaxDeals,
    int MaxIntegrations,
    int MaxAiAgents,
    int MaxApiCallsPerDay)
{
    public static PlanLimitsProfile ForPlan(string planId) => planId switch
    {
        BillingPlans.Starter => new(10, 2000, 5000, 2000, 2, 4, 10_000),
        BillingPlans.Professional => new(50, 20_000, 50_000, 20_000, 5, 8, 100_000),
        BillingPlans.Enterprise => new(500, 500_000, 2_000_000, 500_000, 20, 15, 1_000_000),
        _ => new(5, 500, 1000, 500, 1, 3, 2_000)
    };
}

public record PlanLimitCheckResult(bool Allowed, string? Code, string? Message, int Current, int Limit);

public interface IPlanLimitService
{
    Task<PlanLimitsProfile> GetLimitsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<PlanLimitCheckResult> CheckAsync(Guid tenantId, string resource, CancellationToken cancellationToken = default);
    Task RecordApiCallAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
