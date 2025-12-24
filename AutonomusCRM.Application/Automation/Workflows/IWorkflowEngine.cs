using AutonomusCRM.Domain.Events;

namespace AutonomusCRM.Application.Automation.Workflows;

/// <summary>
/// Interfaz para el motor de workflows
/// </summary>
public interface IWorkflowEngine
{
    Task ExecuteWorkflowsAsync(IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task<bool> EvaluateConditionsAsync(Workflow workflow, IDomainEvent domainEvent, CancellationToken cancellationToken = default);
    Task ExecuteActionsAsync(Workflow workflow, IDomainEvent domainEvent, CancellationToken cancellationToken = default);
}

