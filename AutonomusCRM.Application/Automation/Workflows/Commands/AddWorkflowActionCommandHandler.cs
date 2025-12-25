using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public class AddWorkflowActionCommandHandler : IRequestHandler<AddWorkflowActionCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddWorkflowActionCommandHandler> _logger;

    public AddWorkflowActionCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddWorkflowActionCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(AddWorkflowActionCommand request, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (workflow == null || workflow.TenantId != request.TenantId)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found or tenant mismatch", request.WorkflowId);
            return false;
        }

        var action = new WorkflowAction
        {
            Type = request.Type,
            Target = request.Target,
            Parameters = request.Parameters ?? new Dictionary<string, object>()
        };

        workflow.AddAction(action);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Action added to workflow {WorkflowId}", request.WorkflowId);
        return true;
    }
}

