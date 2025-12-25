using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Agents.Commands;

public record UpdateAgentConfigCommand(
    Guid TenantId,
    string AgentName,
    Dictionary<string, object> Configuration
) : IRequest<bool>;

