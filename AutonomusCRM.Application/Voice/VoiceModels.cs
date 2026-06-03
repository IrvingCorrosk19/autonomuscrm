namespace AutonomusCRM.Application.Voice;

public class VoiceCallLog
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? LeadId { get; private set; }
    public Guid? DealId { get; private set; }
    public Guid? UserId { get; private set; }
    public string Direction { get; private set; } = "outbound";
    public string PhoneNumber { get; private set; } = string.Empty;
    public int DurationSeconds { get; private set; }
    public string Outcome { get; private set; } = "unknown";
    public string? ExternalCallId { get; private set; }
    public string? Provider { get; private set; }
    public string? Notes { get; private set; }
    public string TranscriptStatus { get; private set; } = "pending";
    public string? AiSummary { get; private set; }
    public DateTime StartedAt { get; private set; }
    public DateTime CreatedAt { get; private set; }

    private VoiceCallLog() { }

    public static VoiceCallLog Create(
        Guid tenantId,
        string phoneNumber,
        string direction,
        int durationSeconds,
        string outcome,
        Guid? customerId = null,
        Guid? leadId = null,
        Guid? dealId = null,
        Guid? userId = null,
        string? externalCallId = null,
        string? provider = null,
        string? notes = null)
    {
        return new VoiceCallLog
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            PhoneNumber = phoneNumber,
            Direction = direction,
            DurationSeconds = durationSeconds,
            Outcome = outcome,
            CustomerId = customerId,
            LeadId = leadId,
            DealId = dealId,
            UserId = userId,
            ExternalCallId = externalCallId,
            Provider = provider ?? "manual",
            Notes = notes,
            StartedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void SetAiSummary(string summary) => AiSummary = summary;
    public void SetTranscriptStatus(string status) => TranscriptStatus = status;

    public void UpdateFromWebhook(string status, int? durationSeconds)
    {
        Outcome = status;
        if (durationSeconds.HasValue)
            DurationSeconds = durationSeconds.Value;
        if (status is "completed" or "answered")
            TranscriptStatus = "ready_for_transcription";
    }
}

public record VoiceCallLogDto(
    Guid Id,
    string PhoneNumber,
    string Direction,
    int DurationSeconds,
    string Outcome,
    string? CustomerName,
    string? LeadName,
    string? DealTitle,
    DateTime StartedAt,
    string TranscriptStatus,
    string? AiSummary);

public interface IVoiceCallLogRepository
{
    Task AddAsync(VoiceCallLog log, CancellationToken cancellationToken = default);
    Task<VoiceCallLog?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoiceCallLog>> ListAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default);
}

public interface IVoiceCallService
{
    Task<VoiceCallLog> LogCallAsync(VoiceCallLog log, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VoiceCallLogDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
}
