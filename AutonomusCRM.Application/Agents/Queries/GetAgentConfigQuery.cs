using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Agents.Queries;

public record GetAgentConfigQuery(
    Guid TenantId,
    string AgentName
) : IRequest<Dictionary<string, object>>;

