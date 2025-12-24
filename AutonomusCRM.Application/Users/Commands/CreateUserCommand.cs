using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Users.Commands;

public record CreateUserCommand(
    Guid TenantId,
    string Email,
    string Password,
    string? FirstName = null,
    string? LastName = null
) : IRequest<Guid>;

