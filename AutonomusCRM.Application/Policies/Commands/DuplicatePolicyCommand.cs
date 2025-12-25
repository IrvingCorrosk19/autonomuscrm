using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Policies.Commands;

public record DuplicatePolicyCommand(
    Guid PolicyId,
    Guid TenantId,
    string? NewName = null
) : IRequest<Guid>;

