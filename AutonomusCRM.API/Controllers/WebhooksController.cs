using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Intelligence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/webhooks")]
public class WebhooksController : ControllerBase
{
    /// <summary>Ingest product usage events (HMAC optional via X-Autonomus-Signature).</summary>
    [HttpPost("usage/{tenantId:guid}")]
    [AllowAnonymous]
    public async Task<IActionResult> IngestUsage(
        Guid tenantId,
        [FromBody] JsonElement body,
        CancellationToken cancellationToken)
    {
        if (!await ValidateHmacAsync(cancellationToken))
            return Unauthorized();

        var customerId = body.TryGetProperty("customerId", out var cid) && cid.TryGetGuid(out var g)
            ? g
            : Guid.Empty;
        var feature = body.TryGetProperty("feature", out var f) ? f.GetString() ?? "unknown" : "unknown";
        var quantity = body.TryGetProperty("quantity", out var q) && q.TryGetInt32(out var n) ? n : 1;

        if (customerId == Guid.Empty)
            return BadRequest(new { error = "customerId required" });

        await HttpContext.RequestServices.GetRequiredService<IProductAnalyticsEngine>()
            .RecordUsageAsync(tenantId, feature, "webhook", null, customerId, quantity, cancellationToken);
        return Accepted(new { status = "accepted", tenantId, customerId, feature });
    }

    /// <summary>CRM entity upsert hook — JSON array in "records" for customers.</summary>
    [HttpPost("crm/customers/{tenantId:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<ImportResultDto>> IngestCustomers(
        Guid tenantId,
        [FromBody] WebhookCustomerPayload payload,
        CancellationToken cancellationToken)
    {
        if (!await ValidateHmacAsync(cancellationToken))
            return Unauthorized();
        if (payload.Records == null || payload.Records.Count == 0)
            return BadRequest(new { error = "records required" });

        var guard = ImportGuard.ValidateRowCount(payload.Records.Count);
        if (!guard.Ok) return BadRequest(guard.Error);

        var svc = HttpContext.RequestServices.GetRequiredService<ICrmImportService>();
        return Ok(await svc.ImportCustomersAsync(tenantId, payload.Records, cancellationToken));
    }

    private async Task<bool> ValidateHmacAsync(CancellationToken cancellationToken)
    {
        var config = HttpContext.RequestServices.GetRequiredService<IConfiguration>();
        var secret = config["Webhooks:Secret"];
        if (string.IsNullOrWhiteSpace(secret))
            return true;

        if (!Request.Headers.TryGetValue("X-Autonomus-Signature", out var sigHeader))
            return false;

        Request.EnableBuffering();
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true);
        var body = await reader.ReadToEndAsync(cancellationToken);
        Request.Body.Position = 0;

        var expected = ComputeHmac(body, secret);
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(expected),
            Encoding.UTF8.GetBytes(sigHeader.ToString()));
    }

    private static string ComputeHmac(string body, string secret)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
        var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public sealed class WebhookCustomerPayload
    {
        public List<CustomerImportRow>? Records { get; set; }
    }
}
