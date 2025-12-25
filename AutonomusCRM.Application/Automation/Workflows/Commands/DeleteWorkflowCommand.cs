using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public record DeleteWorkflowCommand(
    Guid WorkflowId,
    Guid TenantId
) : IRequest<bool>;

