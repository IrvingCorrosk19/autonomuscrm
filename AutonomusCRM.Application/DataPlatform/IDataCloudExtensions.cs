namespace AutonomusCRM.Application.DataPlatform;

public interface IIdentityMergeService
{
    Task<int> MergeDuplicatesAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface IWarehouseExportService
{
    Task<byte[]> ExportCustomersCsvAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public interface ICdpEventStreamService
{
    Task PublishAsync(Guid tenantId, string eventType, Guid? customerId, Dictionary<string, object?> payload, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CdpStreamEventDto>> GetRecentAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default);
}

public class CdpStreamEvent
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string EventType { get; private set; } = string.Empty;
    public Guid? CustomerId { get; private set; }
    public Dictionary<string, object?> Payload { get; private set; } = new();
    public DateTime OccurredAt { get; private set; }

    private CdpStreamEvent() { }

    public static CdpStreamEvent Create(Guid tenantId, string eventType, Guid? customerId, Dictionary<string, object?> payload)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            EventType = eventType,
            CustomerId = customerId,
            Payload = payload,
            OccurredAt = DateTime.UtcNow
        };
}

public record CdpStreamEventDto(Guid Id, string EventType, Guid? CustomerId, DateTime OccurredAt, Dictionary<string, object?> Payload);
