using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.Events.EventSourcing.Queries;

public record GetAuditEventsQuery(
    Guid TenantId,
    string? EventType = null,
    DateTime? From = null,
    DateTime? To = null,
    Guid? AggregateId = null,
    int Skip = 0,
    int Take = 100
) : IRequest<IEnumerable<IDomainEvent>>;

