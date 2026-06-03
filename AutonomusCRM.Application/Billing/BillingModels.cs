namespace AutonomusCRM.Application.Billing;

public static class BillingPlans
{
    public const string Free = "free";
    public const string Starter = "starter";
    public const string Professional = "professional";
    public const string Enterprise = "enterprise";
}

public class TenantBillingAccount
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string PlanId { get; private set; } = BillingPlans.Free;
    public string? StripeCustomerId { get; private set; }
    public string? StripeSubscriptionId { get; private set; }
    public string Status { get; private set; } = "trialing";
    public DateTime? CurrentPeriodEnd { get; private set; }
    public int MaxUsers { get; private set; } = 5;
    public int MaxCustomers { get; private set; } = 500;

    private TenantBillingAccount() { }

    public static TenantBillingAccount Create(Guid tenantId, string planId = BillingPlans.Free)
    {
        var (maxUsers, maxCustomers) = PlanLimits(planId);
        return new TenantBillingAccount
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PlanId = planId,
            MaxUsers = maxUsers,
            MaxCustomers = maxCustomers
        };
    }

    public void SetStripeCustomerId(string customerId) => StripeCustomerId = customerId;

    public void ApplyStripe(string customerId, string subscriptionId, string status, DateTime? periodEnd, string planId)
    {
        if (!string.IsNullOrWhiteSpace(customerId)) StripeCustomerId = customerId;
        if (!string.IsNullOrWhiteSpace(subscriptionId)) StripeSubscriptionId = subscriptionId;
        Status = status;
        CurrentPeriodEnd = periodEnd;
        PlanId = planId;
        var limits = PlanLimits(planId);
        MaxUsers = limits.maxUsers;
        MaxCustomers = limits.maxCustomers;
    }

    private static (int maxUsers, int maxCustomers) PlanLimits(string planId) => planId switch
    {
        BillingPlans.Starter => (10, 2000),
        BillingPlans.Professional => (50, 20000),
        BillingPlans.Enterprise => (500, 500000),
        _ => (5, 500)
    };
}

public record CreateCheckoutRequest(string PlanId, string SuccessUrl, string CancelUrl);
public record CheckoutSessionDto(string SessionId, string Url);
