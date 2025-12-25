using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public record DuplicateWorkflowCommand(
    Guid WorkflowId,
    Guid TenantId,
    string? NewName = null
) : IRequest<Guid>;

