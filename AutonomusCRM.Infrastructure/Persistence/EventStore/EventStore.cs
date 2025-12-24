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
        var query = _context.Set<DomainEventRecord>()
            .Where(e => e.TenantId == tenantId);

        if (from.HasValue)
            query = query.Where(e => e.OccurredOn >= from.Value);

        if (to.HasValue)
            query = query.Where(e => e.OccurredOn <= to.Value);

        var records = await query.OrderBy(e => e.OccurredOn).ToListAsync(cancellationToken);
        return DeserializeEvents(records);
    }

    public async Task<List<IDomainEvent>> GetEventsByTypeAsync(string eventType, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        var query = _context.Set<DomainEventRecord>()
            .Where(e => e.EventType == eventType);

        if (tenantId.HasValue)
            query = query.Where(e => e.TenantId == tenantId);

        var records = await query.OrderBy(e => e.OccurredOn).ToListAsync(cancellationToken);
        return DeserializeEvents(records);
    }

    public async Task<List<IDomainEvent>> GetEventsByAggregateIdAsync(Guid aggregateId, int fromVersion = 0, CancellationToken cancellationToken = default)
    {
        var records = await _context.Set<DomainEventRecord>()
            .Where(e => e.AggregateId == aggregateId)
            .OrderBy(e => e.OccurredOn)
            .Skip(fromVersion)
            .ToListAsync(cancellationToken);

        return DeserializeEvents(records);
    }

    private Guid? ExtractAggregateId(IDomainEvent domainEvent)
    {
        // Intentar extraer AggregateId del evento usando reflection
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
        var events = new List<IDomainEvent>();
        foreach (var record in records)
        {
            try
            {
                // TODO: Deserializar según el tipo de evento
                // Por ahora, retornamos eventos básicos
                // Esto requiere un sistema de registro de tipos de eventos
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deserializing event {EventId}", record.Id);
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

