using AutonomusCRM.Application.Billing;
using AutonomusCRM.Application.Integrations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace AutonomusCRM.API.Controllers;

/// <summary>Inbound provider webhooks — signature validation + audit (no fake processing).</summary>
[ApiController]
[Route("api/integrations/webhooks")]
[AllowAnonymous]
public class IntegrationProviderWebhooksController : ControllerBase
{
    private readonly IStripeBillingService _stripe;
    private readonly IIntegrationWebhookAuditor _auditor;
    private readonly IConfiguration _config;

    public IntegrationProviderWebhooksController(
        IStripeBillingService stripe,
        IIntegrationWebhookAuditor auditor,
        IConfiguration config)
    {
        _stripe = stripe;
        _auditor = auditor;
        _config = config;
    }

    [HttpPost("stripe")]
    public async Task<IActionResult> Stripe(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var json = await reader.ReadToEndAsync(cancellationToken);
        var signature = Request.Headers["Stripe-Signature"].ToString();
        var webhookSecret = _config["Stripe:WebhookSecret"];
        var valid = !string.IsNullOrWhiteSpace(webhookSecret) && !string.IsNullOrWhiteSpace(signature);
        _auditor.LogReceived("Stripe", "billing", null, valid, valid ? "signature present" : "missing secret or signature");
        if (!valid)
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Stripe webhook not configured" });
        await _stripe.HandleWebhookAsync(json, signature, cancellationToken);
        return Ok();
    }

    [HttpPost("hubspot/{tenantId:guid}")]
    public async Task<IActionResult> HubSpot(Guid tenantId, CancellationToken cancellationToken)
    {
        var secret = _config["IntegrationWebhooks:HubSpotSecret"];
        var body = await ReadBodyAsync(cancellationToken);
        var valid = ValidateSharedSecret(secret, body);
        _auditor.LogReceived("HubSpot", "crm", tenantId, valid);
        if (!string.IsNullOrWhiteSpace(secret) && !valid) return Unauthorized();
        return Accepted(new { status = "received", provider = "HubSpot", tenantId });
    }

    [HttpPost("salesforce/{tenantId:guid}")]
    public async Task<IActionResult> Salesforce(Guid tenantId, CancellationToken cancellationToken)
    {
        var secret = _config["IntegrationWebhooks:SalesforceSecret"];
        var body = await ReadBodyAsync(cancellationToken);
        var valid = ValidateSharedSecret(secret, body);
        _auditor.LogReceived("Salesforce", "crm", tenantId, valid);
        if (!string.IsNullOrWhiteSpace(secret) && !valid) return Unauthorized();
        return Accepted(new { status = "received", provider = "Salesforce", tenantId });
    }

    [HttpPost("sendgrid")]
    public async Task<IActionResult> SendGrid(CancellationToken cancellationToken)
    {
        var secret = _config["IntegrationWebhooks:SendGridVerificationKey"];
        var body = await ReadBodyAsync(cancellationToken);
        var valid = string.IsNullOrWhiteSpace(secret) || ValidateSharedSecret(secret, body);
        _auditor.LogReceived("SendGrid", "email-event", null, valid);
        if (!string.IsNullOrWhiteSpace(secret) && !valid) return Unauthorized();
        return Accepted(new { status = "received", provider = "SendGrid" });
    }

    [HttpPost("twilio/status")]
    public IActionResult TwilioStatus([FromQuery] Guid tenantId)
    {
        _auditor.LogReceived("Twilio", "status", tenantId, Request.Headers.ContainsKey("X-Twilio-Signature"));
        return Accepted(new { status = "use /api/voice/twilio/status for validated voice webhooks", tenantId });
    }

    [HttpPost("whatsapp")]
    public async Task<IActionResult> WhatsApp(CancellationToken cancellationToken)
    {
        var secret = _config["IntegrationWebhooks:WhatsAppVerifyToken"];
        var mode = Request.Query["hub.mode"].ToString();
        var token = Request.Query["hub.verify_token"].ToString();
        if (mode == "subscribe" && !string.IsNullOrWhiteSpace(secret))
        {
            var valid = token == secret;
            _auditor.LogReceived("WhatsApp", "verify", null, valid);
            return valid ? Content(Request.Query["hub.challenge"].ToString(), "text/plain") : Unauthorized();
        }
        var body = await ReadBodyAsync(cancellationToken);
        var sigValid = ValidateSharedSecret(secret, body);
        _auditor.LogReceived("WhatsApp", "message", null, sigValid);
        return Accepted(new { status = "received", provider = "WhatsApp" });
    }

    private async Task<string> ReadBodyAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private bool ValidateSharedSecret(string? secret, string body)
    {
        if (string.IsNullOrWhiteSpace(secret)) return true;
        if (!Request.Headers.TryGetValue("X-Autonomus-Signature", out var sig)) return false;
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var expected = Convert.ToHexString(hmac.ComputeHash(Encoding.UTF8.GetBytes(body))).ToLowerInvariant();
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(sig.ToString()));
    }
}
