using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public class UpdateWorkflowCommandHandler : IRequestHandler<UpdateWorkflowCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateWorkflowCommandHandler> _logger;

    public UpdateWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateWorkflowCommand request, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (workflow == null || workflow.TenantId != request.TenantId)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found or tenant mismatch", request.WorkflowId);
            throw new InvalidOperationException("Workflow no encontrado o no pertenece al tenant");
        }

        if (workflow.Name != request.Name || workflow.Description != request.Description)
        {
            workflow.UpdateInfo(request.Name, request.Description);
        }

        if (request.IsActive.HasValue && workflow.IsActive != request.IsActive.Value)
        {
            workflow.SetActive(request.IsActive.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Workflow {WorkflowId} updated successfully", request.WorkflowId);
        
        return true;
    }
}

