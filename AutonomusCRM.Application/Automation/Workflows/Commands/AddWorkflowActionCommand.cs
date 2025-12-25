using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public record AddWorkflowActionCommand(
    Guid WorkflowId,
    Guid TenantId,
    string Type,
    string Target,
    Dictionary<string, object>? Parameters = null
) : IRequest<bool>;

