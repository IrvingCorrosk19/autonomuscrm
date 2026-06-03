namespace AutonomusCRM.Application.Billing;

public record BillingUsageDto(int Users, int Customers, int Leads, int Deals, int Integrations);

public record BillingDashboardDto(
    TenantBillingAccount Account,
    PlanLimitsProfile Limits,
    BillingUsageDto Usage,
    bool StripeConfigured);

public interface IBillingDashboardService
{
    Task<BillingDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
