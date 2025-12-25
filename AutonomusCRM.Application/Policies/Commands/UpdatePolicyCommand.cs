using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Policies.Commands;

public record UpdatePolicyCommand(
    Guid PolicyId,
    Guid TenantId,
    string Name,
    string Expression,
    string? Description = null,
    bool? IsActive = null
) : IRequest<bool>;

