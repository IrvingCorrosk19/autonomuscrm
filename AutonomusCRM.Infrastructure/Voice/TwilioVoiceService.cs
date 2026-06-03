using System.Security.Cryptography;
using System.Text;
using AutonomusCRM.Application.Voice;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Voice;

public sealed class TwilioVoiceService : ITwilioVoiceService
{
    private readonly ApplicationDbContext _db;
    private readonly IVoiceCallService _voice;
    private readonly string? _authToken;
    private readonly ILogger<TwilioVoiceService> _logger;

    public TwilioVoiceService(
        ApplicationDbContext db,
        IVoiceCallService voice,
        IConfiguration configuration,
        ILogger<TwilioVoiceService> logger)
    {
        _db = db;
        _voice = voice;
        _authToken = configuration["Twilio:AuthToken"];
        _logger = logger;
    }

    public async Task<Guid?> HandleCallStatusWebhookAsync(
        Guid tenantId, TwilioCallWebhookPayload payload, CancellationToken cancellationToken = default)
    {
        var existing = await _db.VoiceCallLogs
            .FirstOrDefaultAsync(v => v.TenantId == tenantId && v.ExternalCallId == payload.CallSid, cancellationToken);

        if (existing != null)
        {
            existing.UpdateFromWebhook(payload.CallStatus, payload.CallDuration);
            await _db.SaveChangesAsync(cancellationToken);
            return existing.Id;
        }

        var log = VoiceCallLog.Create(
            tenantId, payload.From, "inbound", payload.CallDuration ?? 0, payload.CallStatus,
            externalCallId: payload.CallSid, provider: "twilio", notes: $"To={payload.To}");
        await _voice.LogCallAsync(log, cancellationToken);
        _logger.LogInformation("Twilio call {Sid} status {Status}", payload.CallSid, payload.CallStatus);
        return log.Id;
    }

    public bool ValidateWebhookSignature(string url, Dictionary<string, string> form, string signature)
    {
        if (string.IsNullOrWhiteSpace(_authToken) || string.IsNullOrWhiteSpace(signature))
            return string.IsNullOrWhiteSpace(_authToken);

        var sorted = form.OrderBy(kv => kv.Key, StringComparer.Ordinal);
        var data = url + string.Concat(sorted.Select(kv => kv.Key + kv.Value));
        using var hmac = new HMACSHA1(Encoding.UTF8.GetBytes(_authToken));
        var hash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(data)));
        return hash == signature;
    }
}
