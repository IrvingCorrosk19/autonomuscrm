using AutonomusCRM.Application.Billing;
using AutonomusCRM.Application.Common.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/billing")]
public class BillingController : ControllerBase
{
    private readonly IStripeBillingService _billing;
    private readonly ITenantContext _tenant;

    public BillingController(IStripeBillingService billing, ITenantContext tenant)
    {
        _billing = billing;
        _tenant = tenant;
    }

    [HttpGet("account")]
    public async Task<IActionResult> GetAccount(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _billing.GetOrCreateAccountAsync(tenantId, cancellationToken));
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout([FromBody] CreateCheckoutRequest request, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _billing.CreateCheckoutSessionAsync(tenantId, request, cancellationToken));
    }

    [HttpPost("stripe/webhook")]
    [AllowAnonymous]
    public async Task<IActionResult> StripeWebhook(CancellationToken cancellationToken)
    {
        var json = await new StreamReader(Request.Body).ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        await _billing.HandleWebhookAsync(json, signature, cancellationToken);
        return Ok();
    }
}
