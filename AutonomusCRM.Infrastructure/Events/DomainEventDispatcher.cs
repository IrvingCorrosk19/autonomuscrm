using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Infrastructure.Persistence.EventStore;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IEventBus _eventBus;
    private readonly EventStore _eventStore;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IEventBus eventBus,
        EventStore eventStore,
        IWorkflowEngine workflowEngine,
        ILogger<DomainEventDispatcher> logger)
    {
        _eventBus = eventBus;
        _eventStore = eventStore;
        _workflowEngine = workflowEngine;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Dispatching domain event: {EventType} (Id: {EventId}, TenantId: {TenantId}, CorrelationId: {CorrelationId})",
            domainEvent.EventType,
            domainEvent.Id,
            domainEvent.TenantId,
            domainEvent.CorrelationId);

        // Guardar en Event Store
        await _eventStore.SaveEventAsync(domainEvent, cancellationToken);

        // Ejecutar workflows
        await _workflowEngine.ExecuteWorkflowsAsync(domainEvent, cancellationToken);

        // Publicar en Event Bus
        await _eventBus.PublishAsync(domainEvent, cancellationToken);
    }

    public async Task DispatchAsync(IEnumerable<IDomainEvent> domainEvents, CancellationToken cancellationToken = default)
    {
        foreach (var domainEvent in domainEvents)
        {
            await DispatchAsync(domainEvent, cancellationToken);
        }
    }
}

