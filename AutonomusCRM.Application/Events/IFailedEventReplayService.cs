namespace AutonomusCRM.Application.Events;

public record FailedEventListItemDto(
    Guid Id,
    Guid? TenantId,
    string MessageId,
    string EventType,
    DateTime FailedAt,
    int RetryCount,
    string? LastError);

public interface IFailedEventReplayService
{
    Task<IReadOnlyList<FailedEventListItemDto>> ListAsync(Guid tenantId, int take = 50, CancellationToken cancellationToken = default);
    Task<bool> MarkReplayRequestedAsync(Guid tenantId, Guid id, CancellationToken cancellationToken = default);
}
