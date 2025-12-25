using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Policies.Commands;

public record CreatePolicyCommand(
    Guid TenantId,
    string Name,
    string Expression,
    string? Description = null
) : IRequest<Guid>;

