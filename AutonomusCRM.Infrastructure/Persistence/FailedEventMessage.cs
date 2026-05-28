namespace AutonomusCRM.Infrastructure.Persistence;

/// <summary>
/// Mensaje de evento que falló tras reintentos (dead-letter persistido).
/// </summary>
public class FailedEventMessage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid? TenantId { get; set; }
    public string MessageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string RoutingKey { get; set; } = string.Empty;
    public string Payload { get; set; } = string.Empty;
    public string? Error { get; set; }
    public int RetryCount { get; set; }
    public DateTime FailedAt { get; set; } = DateTime.UtcNow;
}
