using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Users.Commands;

public record RemoveUserRoleCommand(
    Guid UserId,
    Guid TenantId,
    string Role
) : IRequest<bool>;

