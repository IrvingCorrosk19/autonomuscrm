using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Automation.Workflows;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public class CreateWorkflowCommandHandler : IRequestHandler<CreateWorkflowCommand, Guid>
{
    private readonly IWorkflowRepository _workflowRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreateWorkflowCommandHandler> _logger;

    public CreateWorkflowCommandHandler(
        IWorkflowRepository workflowRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreateWorkflowCommandHandler> logger)
    {
        _workflowRepository = workflowRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(CreateWorkflowCommand request, CancellationToken cancellationToken = default)
    {
        var workflow = Workflow.Create(request.TenantId, request.Name, request.Description);
        
        await _workflowRepository.AddAsync(workflow, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Workflow {WorkflowId} created successfully", workflow.Id);
        return workflow.Id;
    }
}

