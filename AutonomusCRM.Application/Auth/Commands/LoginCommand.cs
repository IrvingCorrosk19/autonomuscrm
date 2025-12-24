using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Auth.Commands;

public record LoginCommand(
    string Email,
    string Password,
    Guid TenantId
) : IRequest<LoginResult>;

public record LoginResult(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    bool RequiresMfa
);

