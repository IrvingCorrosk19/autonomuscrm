using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Events.EventSourcing.Queries;

public class GetAuditEventsQueryHandler : IRequestHandler<GetAuditEventsQuery, IEnumerable<IDomainEvent>>
{
    private readonly IEventStore _eventStore;
    private readonly ILogger<GetAuditEventsQueryHandler> _logger;

    public GetAuditEventsQueryHandler(
        IEventStore eventStore,
        ILogger<GetAuditEventsQueryHandler> logger)
    {
        _eventStore = eventStore;
        _logger = logger;
    }

    public async Task<IEnumerable<IDomainEvent>> HandleAsync(GetAuditEventsQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            IEnumerable<IDomainEvent> events;

            if (request.AggregateId.HasValue)
            {
                events = await _eventStore.GetEventsByAggregateIdAsync(request.AggregateId.Value, 0, cancellationToken);
                return events.Skip(request.Skip).Take(request.Take);
            }

            return await _eventStore.GetEventsByTenantPagedAsync(
                request.TenantId,
                request.From,
                request.To,
                request.EventType,
                request.Skip,
                request.Take,
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting audit events");
            return Enumerable.Empty<IDomainEvent>();
        }
    }
}

