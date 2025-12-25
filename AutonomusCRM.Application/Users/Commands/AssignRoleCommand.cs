using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Users.Commands;

public record AssignRoleCommand(
    Guid UserId,
    Guid TenantId,
    string Role
) : IRequest<bool>;

