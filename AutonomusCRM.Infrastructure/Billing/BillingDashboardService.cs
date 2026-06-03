using AutonomusCRM.Application.Billing;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AutonomusCRM.Infrastructure.Billing;

public sealed class BillingDashboardService : IBillingDashboardService
{
    private readonly ApplicationDbContext _db;
    private readonly IStripeBillingService _stripe;
    private readonly IPlanLimitService _limits;
    private readonly IConfiguration _config;

    public BillingDashboardService(
        ApplicationDbContext db,
        IStripeBillingService stripe,
        IPlanLimitService limits,
        IConfiguration config)
    {
        _db = db;
        _stripe = stripe;
        _limits = limits;
        _config = config;
    }

    public async Task<BillingDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var account = await _stripe.GetOrCreateAccountAsync(tenantId, cancellationToken);
        var limits = await _limits.GetLimitsAsync(tenantId, cancellationToken);
        var usage = new BillingUsageDto(
            await _db.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken),
            await _db.Customers.CountAsync(c => c.TenantId == tenantId, cancellationToken),
            await _db.Leads.CountAsync(l => l.TenantId == tenantId, cancellationToken),
            await _db.Deals.CountAsync(d => d.TenantId == tenantId, cancellationToken),
            await _db.TenantIntegrations.CountAsync(i => i.TenantId == tenantId && i.IsEnabled, cancellationToken));

        var stripeKey = _config["Stripe:SecretKey"] ?? _config["Stripe__SecretKey"];
        var configured = !string.IsNullOrWhiteSpace(stripeKey);

        return new BillingDashboardDto(account, limits, usage, configured);
    }
}
