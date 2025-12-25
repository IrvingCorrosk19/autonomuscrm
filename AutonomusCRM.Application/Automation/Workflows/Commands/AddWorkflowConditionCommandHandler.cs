using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public class AddWorkflowConditionCommandHandler : IRequestHandler<AddWorkflowConditionCommand, bool>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AddWorkflowConditionCommandHandler> _logger;

    public AddWorkflowConditionCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        ILogger<AddWorkflowConditionCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(AddWorkflowConditionCommand request, CancellationToken cancellationToken = default)
    {
        var workflow = await _workflowRepository.GetByIdAsync(request.WorkflowId, cancellationToken);
        
        if (workflow == null || workflow.TenantId != request.TenantId)
        {
            _logger.LogWarning("Workflow {WorkflowId} not found or tenant mismatch", request.WorkflowId);
            return false;
        }

        var condition = new WorkflowCondition
        {
            Type = request.Type,
            Expression = request.Expression,
            Parameters = request.Parameters ?? new Dictionary<string, object>()
        };

        workflow.AddCondition(condition);
        await _workflowRepository.UpdateAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Condition added to workflow {WorkflowId}", request.WorkflowId);
        return true;
    }
}

