using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using AutonomusCRM.Application.Events.EventSourcing;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Events;

public class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IEventBus _eventBus;
    private readonly IEventStore _eventStore;
    private readonly IWorkflowEngine _workflowEngine;
    private readonly IOperationalAutomationService _operationalAutomation;
    private readonly Application.Revenue.IRevenueAutomationEngine _revenueAutomation;
    private readonly Application.CustomerSuccess.IRetentionAutomationEngine _retentionAutomation;
    private readonly ILogger<DomainEventDispatcher> _logger;

    public DomainEventDispatcher(
        IEventBus eventBus,
        IEventStore eventStore,
        IWorkflowEngine workflowEngine,
        IOperationalAutomationService operationalAutomation,
        Application.Revenue.IRevenueAutomationEngine revenueAutomation,
        Application.CustomerSuccess.IRetentionAutomationEngine retentionAutomation,
        ILogger<DomainEventDispatcher> logger)
    {
        _eventBus = eventBus;
        _eventStore = eventStore;
        _workflowEngine = workflowEngine;
        _operationalAutomation = operationalAutomation;
        _revenueAutomation = revenueAutomation;
        _retentionAutomation = retentionAutomation;
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

        // Automatización operativa P0 (lead calificado, onboarding CS, etc.)
        await _operationalAutomation.ProcessEventAsync(domainEvent, cancellationToken);
        await _revenueAutomation.ProcessEventAsync(domainEvent, cancellationToken);
        await _retentionAutomation.ProcessEventAsync(domainEvent, cancellationToken);

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

