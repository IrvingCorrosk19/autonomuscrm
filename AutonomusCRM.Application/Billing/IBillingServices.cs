namespace AutonomusCRM.Application.Billing;

public interface ITenantBillingRepository
{
    Task<TenantBillingAccount?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<TenantBillingAccount?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default);
    Task UpsertAsync(TenantBillingAccount account, CancellationToken cancellationToken = default);
}

public interface IStripeBillingService
{
    Task<CheckoutSessionDto> CreateCheckoutSessionAsync(Guid tenantId, CreateCheckoutRequest request, CancellationToken cancellationToken = default);
    Task HandleWebhookAsync(string json, string stripeSignature, CancellationToken cancellationToken = default);
    Task<TenantBillingAccount> GetOrCreateAccountAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
