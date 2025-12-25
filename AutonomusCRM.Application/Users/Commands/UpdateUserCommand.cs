using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Users.Commands;

public record UpdateUserCommand(
    Guid UserId,
    Guid TenantId,
    string? FirstName = null,
    string? LastName = null,
    string? Email = null,
    bool? IsActive = null
) : IRequest<bool>;

