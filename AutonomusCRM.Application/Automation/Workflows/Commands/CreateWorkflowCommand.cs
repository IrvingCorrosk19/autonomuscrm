using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public record CreateWorkflowCommand(
    Guid TenantId,
    string Name,
    string? Description = null
) : IRequest<Guid>;

