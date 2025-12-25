using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public class DeleteWorkflowCommandHandler : IRequestHandler<DeleteWorkflowCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteWorkflowCommandHandler> _logger;

    public DeleteWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeleteWorkflowCommand request, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (workflow == null || workflow.TenantId != request.TenantId)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found or tenant mismatch", request.WorkflowId);
            throw new InvalidOperationException("Workflow no encontrado o no pertenece al tenant");
        }

        await _workflowRepository.DeleteAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Workflow {WorkflowId} deleted successfully", request.WorkflowId);
        return true;
    }
}

