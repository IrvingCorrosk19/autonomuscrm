using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Users.Commands;

public record ToggleUserStatusCommand(
    Guid UserId,
    Guid TenantId,
    bool IsActive
) : IRequest<bool>;

