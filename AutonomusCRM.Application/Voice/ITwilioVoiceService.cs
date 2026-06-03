namespace AutonomusCRM.Application.Voice;

public record TwilioCallWebhookPayload(string CallSid, string From, string To, string CallStatus, int? CallDuration);

public interface ITwilioVoiceService
{
    Task<Guid?> HandleCallStatusWebhookAsync(Guid tenantId, TwilioCallWebhookPayload payload, CancellationToken cancellationToken = default);
    bool ValidateWebhookSignature(string url, Dictionary<string, string> form, string signature);
}
