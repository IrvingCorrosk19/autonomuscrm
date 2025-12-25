using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public record UpdateWorkflowCommand(
    Guid WorkflowId,
    Guid TenantId,
    string Name,
    string? Description = null,
    bool? IsActive = null
) : IRequest<bool>;

