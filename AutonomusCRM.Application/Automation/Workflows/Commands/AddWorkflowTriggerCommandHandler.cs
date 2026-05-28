using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public class AddWorkflowTriggerCommandHandler : IRequestHandler<AddWorkflowTriggerCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddWorkflowTriggerCommandHandler> _logger;

    public AddWorkflowTriggerCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddWorkflowTriggerCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(AddWorkflowTriggerCommand request, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (workflow == null || workflow.TenantId != request.TenantId)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found or tenant mismatch", request.WorkflowId);
            return false;
        }

        if (!string.Equals(request.Type, "DomainEvent", StringComparison.Ordinal))
        {
            _logger.LogWarning("Trigger type {Type} is not supported by the workflow engine", request.Type);
            return false;
        }

        if (string.IsNullOrWhiteSpace(request.EventType))
            return false;

        var trigger = new WorkflowTrigger
        {
            Type = "DomainEvent",
            EventType = request.EventType.Trim(),
            Parameters = request.Parameters ?? new Dictionary<string, object>()
        };

        workflow.AddTrigger(trigger);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Trigger added to workflow {WorkflowId}", request.WorkflowId);
        return true;
    }
}

