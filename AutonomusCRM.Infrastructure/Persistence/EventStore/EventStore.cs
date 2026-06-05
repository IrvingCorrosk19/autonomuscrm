using AutonomusCRM.Domain.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

using AutonomusCRM.Application.Events.EventSourcing;

namespace AutonomusCRM.Infrastructure.Persistence.EventStore;

/// <summary>
/// Event Store para registrar todos los eventos de dominio (Event Sourcing)
/// </summary>
public class EventStore : IEventStore
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<EventStore> _logger;

    public EventStore(ApplicationDbContext context, ILogger<EventStore> logger)
    {
        _context = context;
        _logger = logger;
    }

    private IQueryable<DomainEventRecord> Records => _context.Set<DomainEventRecord>().AsNoTracking();

    public async Task SaveEventAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        var eventRecord = new DomainEventRecord
        {
            Id = domainEvent.Id,
            EventType = domainEvent.EventType,
            TenantId = domainEvent.TenantId,
            CorrelationId = domainEvent.CorrelationId,
            OccurredOn = domainEvent.OccurredOn,
            EventData = JsonSerializer.Serialize(domainEvent),
            CreatedAt = DateTime.UtcNow,
            AggregateId = ExtractAggregateId(domainEvent)
        };

        _context.Set<DomainEventRecord>().Add(eventRecord);
        await _context.SaveChangesAsync(cancellationToken);

        _logger.LogDebug(
            "Event stored: {EventType} (Id: {EventId}, TenantId: {TenantId})",
            domainEvent.EventType,
            domainEvent.Id,
            domainEvent.TenantId);
    }

    public async Task<List<IDomainEvent>> GetEventsByTenantAsync(Guid tenantId, DateTime? from = null, DateTime? to = null, CancellationToken cancellationToken = default)
    {
        return await GetEventsByTenantPagedAsync(tenantId, from, to, null, 0, int.MaxValue, cancellationToken);
    }

    public async Task<List<IDomainEvent>> GetEventsByTenantPagedAsync(
        Guid tenantId,
        DateTime? from,
        DateTime? to,
        string? eventType,
        int skip,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = Records.Where(e => e.TenantId == tenantId);

        if (from.HasValue)
            query = query.Where(e => e.OccurredOn >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredOn <= to.Value);

        if (!string.IsNullOrWhiteSpace(eventType))
            query = query.Where(e => e.EventType == eventType);

        var records = await query
            .OrderByDescending(e => e.OccurredOn)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

        return DeserializeEvents(records);
    }

    public async Task<List<IDomainEvent>> GetEventsByTypeAsync(string eventType, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = Records.Where(e => e.EventType == eventType);

        if (tenantId.HasValue)
            query = query.Where(e => e.TenantId == tenantId);

        var records = await query.OrderByDescending(e => e.OccurredOn).ToListAsync(cancellationToken);
        return DeserializeEvents(records);
    }

    public async Task<List<IDomainEvent>> GetEventsByAggregateIdAsync(Guid aggregateId, int fromVersion = 0, CancellationToken cancellationToken = default)
    {
        var records = await Records
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.OccurredOn)
            .Skip(fromVersion)
            .ToListAsync(cancellationToken);

        return DeserializeEvents(records);
    }

    public async Task<int> CountByTenantAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await Records.CountAsync(e => e.TenantId == tenantId, cancellationToken);
    }

    public async Task<int> CountByTenantInRangeAsync(Guid tenantId, DateTime from, DateTime to, CancellationToken cancellationToken = default)
    {
        return await Records.CountAsync(
            e => e.TenantId == tenantId && e.OccurredOn >= from && e.OccurredOn < to,
            cancellationToken);
    }

    public async Task<List<string>> GetDistinctEventTypesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        return await Records
            .Where(e => e.TenantId == tenantId)
            .Select(e => e.EventType)
            .Distinct()
            .OrderBy(e => e)
            .ToListAsync(cancellationToken);
    }

    private Guid? ExtractAggregateId(IDomainEvent domainEvent)
    {
        var property = domainEvent.GetType().GetProperty("AggregateId") 
            ?? domainEvent.GetType().GetProperty("TenantId")
            ?? domainEvent.GetType().GetProperty("CustomerId")
            ?? domainEvent.GetType().GetProperty("LeadId")
            ?? domainEvent.GetType().GetProperty("DealId");

        if (property != null && property.GetValue(domainEvent) is Guid id)
            return id;

        return null;
    }

    private List<IDomainEvent> DeserializeEvents(List<DomainEventRecord> records)
    {
        var events = new List<IDomainEvent>(records.Count);
        foreach (var record in records)
        {
            try
            {
                if (DomainEventTypeRegistry.TryDeserialize(record.EventType, record.EventData, out var domainEvent))
                {
                    events.Add(domainEvent);
                    continue;
                }

                events.Add(PersistedDomainEvent.FromRecord(record));
                _logger.LogDebug("Event {EventType} materialized as persisted envelope", record.EventType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing event {EventId}", record.Id);
                events.Add(PersistedDomainEvent.FromRecord(record));
            }
        }
        return events;
    }
}

public class DomainEventRecord
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public Guid? CorrelationId { get; set; }
    public Guid? AggregateId { get; set; }
    public DateTime OccurredOn { get; set; }
    public string EventData { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
