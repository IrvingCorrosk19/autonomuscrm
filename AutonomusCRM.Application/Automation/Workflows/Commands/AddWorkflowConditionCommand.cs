using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Automation.Workflows.Commands;

public record AddWorkflowConditionCommand(
    Guid WorkflowId,
    Guid TenantId,
    string Type,
    string Expression,
    Dictionary<string, object>? Parameters = null
) : IRequest<bool>;

