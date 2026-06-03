using AutonomusCRM.Application.Voice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/voice/twilio")]
[AllowAnonymous]
public class VoiceWebhookController : ControllerBase
{
    private readonly ITwilioVoiceService _twilio;

    public VoiceWebhookController(ITwilioVoiceService twilio) => _twilio = twilio;

    [HttpPost("status")]
    public async Task<IActionResult> Status([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var form = Request.Form.ToDictionary(k => k.Key, v => v.Value.ToString());
        var signature = Request.Headers["X-Twilio-Signature"].ToString();
        if (!_twilio.ValidateWebhookSignature(Request.Scheme + "://" + Request.Host + Request.Path + Request.QueryString, form, signature))
            return Unauthorized();

        var payload = new TwilioCallWebhookPayload(
            form.GetValueOrDefault("CallSid", ""),
            form.GetValueOrDefault("From", ""),
            form.GetValueOrDefault("To", ""),
            form.GetValueOrDefault("CallStatus", "unknown"),
            int.TryParse(form.GetValueOrDefault("CallDuration", "0"), out var d) ? d : null);

        var id = await _twilio.HandleCallStatusWebhookAsync(tenantId, payload, cancellationToken);
        return Ok(new { callLogId = id });
    }
}
