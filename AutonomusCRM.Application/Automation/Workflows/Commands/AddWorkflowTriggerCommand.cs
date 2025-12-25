using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public record AddWorkflowTriggerCommand(
    Guid WorkflowId,
    Guid TenantId,
    string Type,
    string EventType,
    Dictionary<string, object>? Parameters = null
) : IRequest<bool>;

