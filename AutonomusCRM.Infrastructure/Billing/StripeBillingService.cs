using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Billing;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Stripe;
using Stripe.Checkout;

namespace AutonomusCRM.Infrastructure.Billing;

public sealed class StripeBillingOptions
{
    public const string SectionName = "Stripe";
    public string? SecretKey { get; set; }
    public string? WebhookSecret { get; set; }
    public string? PriceStarter { get; set; }
    public string? PriceProfessional { get; set; }
    public string? PriceEnterprise { get; set; }
}

public sealed class TenantBillingRepository : ITenantBillingRepository
{
    private readonly ApplicationDbContext _db;

    public TenantBillingRepository(ApplicationDbContext db) => _db = db;

    public Task<TenantBillingAccount?> GetByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => _db.TenantBillingAccounts.FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);

    public Task<TenantBillingAccount?> GetByStripeCustomerIdAsync(string stripeCustomerId, CancellationToken cancellationToken = default)
        => _db.TenantBillingAccounts.FirstOrDefaultAsync(x => x.StripeCustomerId == stripeCustomerId, cancellationToken);

    public async Task UpsertAsync(TenantBillingAccount account, CancellationToken cancellationToken = default)
    {
        var existing = await GetByTenantAsync(account.TenantId, cancellationToken);
        if (existing == null)
            await _db.TenantBillingAccounts.AddAsync(account, cancellationToken);
        else
            existing.ApplyStripe(account.StripeCustomerId ?? existing.StripeCustomerId ?? "",
                account.StripeSubscriptionId ?? "", account.Status, account.CurrentPeriodEnd, account.PlanId);
        await _db.SaveChangesAsync(cancellationToken);
    }
}

public sealed class StripeBillingService : IStripeBillingService
{
    private readonly StripeBillingOptions _options;
    private readonly ITenantBillingRepository _billing;
    private readonly ITenantRepository _tenants;
    private readonly ICustomerRepository _customers;
    private readonly IOutcomeAttributionService _outcomes;
    private readonly ILogger<StripeBillingService> _logger;

    public StripeBillingService(
        IConfiguration configuration,
        ITenantBillingRepository billing,
        ITenantRepository tenants,
        ICustomerRepository customers,
        IOutcomeAttributionService outcomes,
        ILogger<StripeBillingService> logger)
    {
        _options = configuration.GetSection(StripeBillingOptions.SectionName).Get<StripeBillingOptions>() ?? new();
        _billing = billing;
        _tenants = tenants;
        _customers = customers;
        _outcomes = outcomes;
        _logger = logger;
        if (!string.IsNullOrWhiteSpace(_options.SecretKey))
            StripeConfiguration.ApiKey = _options.SecretKey;
    }

    public async Task<TenantBillingAccount> GetOrCreateAccountAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var account = await _billing.GetByTenantAsync(tenantId, cancellationToken);
        if (account != null) return account;
        account = TenantBillingAccount.Create(tenantId);
        await _billing.UpsertAsync(account, cancellationToken);
        return account;
    }

    public async Task<CheckoutSessionDto> CreateCheckoutSessionAsync(
        Guid tenantId, CreateCheckoutRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SecretKey))
            throw new InvalidOperationException("Stripe not configured");

        var priceId = ResolvePriceId(request.PlanId);
        var account = await GetOrCreateAccountAsync(tenantId, cancellationToken);
        var tenant = await _tenants.GetByIdAsync(tenantId, cancellationToken)
            ?? throw new InvalidOperationException("Tenant not found");

        var customerService = new CustomerService();
        if (string.IsNullOrWhiteSpace(account.StripeCustomerId))
        {
            var customer = await customerService.CreateAsync(new CustomerCreateOptions
            {
                Name = tenant.Name,
                Metadata = new Dictionary<string, string> { ["tenantId"] = tenantId.ToString() }
            }, cancellationToken: cancellationToken);
            account.SetStripeCustomerId(customer.Id);
            await _billing.UpsertAsync(account, cancellationToken);
        }

        var sessionService = new SessionService();
        var session = await sessionService.CreateAsync(new SessionCreateOptions
        {
            Mode = "subscription",
            Customer = account.StripeCustomerId,
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            LineItems = new List<SessionLineItemOptions>
            {
                new() { Price = priceId, Quantity = 1 }
            },
            Metadata = new Dictionary<string, string>
            {
                ["tenantId"] = tenantId.ToString(),
                ["planId"] = request.PlanId
            }
        }, cancellationToken: cancellationToken);

        return new CheckoutSessionDto(session.Id, session.Url ?? "");
    }

    public async Task HandleWebhookAsync(string json, string stripeSignature, CancellationToken cancellationToken = default)
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";
        if (string.IsNullOrWhiteSpace(_options.WebhookSecret))
        {
            if (string.Equals(env, "Production", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Stripe WebhookSecret es obligatorio en Production.");
            if (string.IsNullOrWhiteSpace(stripeSignature))
                throw new UnauthorizedAccessException("Stripe-Signature requerida.");
        }

        Event stripeEvent;
        if (!string.IsNullOrWhiteSpace(_options.WebhookSecret))
            stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _options.WebhookSecret);
        else
            stripeEvent = EventUtility.ParseEvent(json);

        switch (stripeEvent.Type)
        {
            case "checkout.session.completed":
                await OnCheckoutCompleted(stripeEvent, cancellationToken);
                break;
            case "customer.subscription.updated":
            case "customer.subscription.deleted":
                await OnSubscriptionChanged(stripeEvent, cancellationToken);
                break;
            case "invoice.paid":
                await OnInvoicePaid(stripeEvent, cancellationToken);
                break;
        }
    }

    private async Task OnCheckoutCompleted(Event stripeEvent, CancellationToken cancellationToken)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session?.Metadata == null || !session.Metadata.TryGetValue("tenantId", out var tidStr)) return;
        if (!Guid.TryParse(tidStr, out var tenantId)) return;

        var account = await GetOrCreateAccountAsync(tenantId, cancellationToken);
        var planId = session.Metadata.GetValueOrDefault("planId") ?? BillingPlans.Starter;
        account.ApplyStripe(session.CustomerId, session.SubscriptionId ?? "", "active",
            DateTime.UtcNow.AddMonths(1), planId);
        await _billing.UpsertAsync(account, cancellationToken);
        _logger.LogInformation("Stripe checkout completed tenant {TenantId} plan {Plan}", tenantId, planId);
    }

    private async Task OnSubscriptionChanged(Event stripeEvent, CancellationToken cancellationToken)
    {
        var sub = stripeEvent.Data.Object as Subscription;
        if (sub?.Metadata == null || !sub.Metadata.TryGetValue("tenantId", out var tidStr)) return;
        if (!Guid.TryParse(tidStr, out var tenantId)) return;

        var account = await GetOrCreateAccountAsync(tenantId, cancellationToken);
        var status = sub.Status ?? "active";
        account.ApplyStripe(sub.CustomerId, sub.Id, status, sub.CurrentPeriodEnd, account.PlanId);
        await _billing.UpsertAsync(account, cancellationToken);
    }

    private async Task OnInvoicePaid(Event stripeEvent, CancellationToken cancellationToken)
    {
        var invoice = stripeEvent.Data.Object as Invoice;
        if (invoice?.CustomerId == null) return;

        var account = await _billing.GetByStripeCustomerIdAsync(invoice.CustomerId, cancellationToken);
        if (account == null) return;

        var customers = await _customers.GetAllAsync(cancellationToken);
        var customer = customers.FirstOrDefault();
        if (customer == null) return;

        await _outcomes.AttributePaymentAsync(
            account.TenantId, customer.Id, invoice.AmountPaid / 100m, true,
            $"Stripe invoice paid {invoice.Id}", cancellationToken);
    }

    private string ResolvePriceId(string planId) => planId switch
    {
        BillingPlans.Professional => _options.PriceProfessional ?? throw new InvalidOperationException("PriceProfessional not set"),
        BillingPlans.Enterprise => _options.PriceEnterprise ?? throw new InvalidOperationException("PriceEnterprise not set"),
        _ => _options.PriceStarter ?? throw new InvalidOperationException("PriceStarter not set")
    };
}
