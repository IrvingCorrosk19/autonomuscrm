using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Policies.Commands;

public record DeletePolicyCommand(
    Guid PolicyId,
    Guid TenantId
) : IRequest<bool>;

