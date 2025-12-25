using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public class DuplicateWorkflowCommandHandler : IRequestHandler<DuplicateWorkflowCommand, Guid>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DuplicateWorkflowCommandHandler> _logger;

    public DuplicateWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        ILogger<DuplicateWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(DuplicateWorkflowCommand request, CancellationToken cancellationToken = default)
    {
        var originalWorkflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (originalWorkflow == null || originalWorkflow.TenantId != request.TenantId)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found or tenant mismatch", request.WorkflowId);
            throw new InvalidOperationException("Workflow no encontrado o no pertenece al tenant");
        }

        var newName = request.NewName ?? $"{originalWorkflow.Name} (Copia)";
        var newWorkflow = Workflow.Create(request.TenantId, newName, originalWorkflow.Description);
        
        // Copiar triggers, condiciones y acciones
        foreach (var trigger in originalWorkflow.Triggers)
        {
            newWorkflow.AddTrigger(new WorkflowTrigger
            {
                Type = trigger.Type,
                EventType = trigger.EventType,
                Parameters = new Dictionary<string, object>(trigger.Parameters)
            });
        }
        
        foreach (var condition in originalWorkflow.Conditions)
        {
            newWorkflow.AddCondition(new WorkflowCondition
            {
                Type = condition.Type,
                Expression = condition.Expression,
                Parameters = new Dictionary<string, object>(condition.Parameters)
            });
        }
        
        foreach (var action in originalWorkflow.Actions)
        {
            newWorkflow.AddAction(new WorkflowAction
            {
                Type = action.Type,
                Target = action.Target,
                Parameters = new Dictionary<string, object>(action.Parameters)
            });
        }
        
        await _workflowRepository.AddAsync(newWorkflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Workflow {WorkflowId} duplicated to {NewWorkflowId}", request.WorkflowId, newWorkflow.Id);
        return newWorkflow.Id;
    }
}

