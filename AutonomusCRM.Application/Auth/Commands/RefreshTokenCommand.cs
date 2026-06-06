using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Common.Localization;

namespace AutonomusCRM.Application.Auth.Commands;

public record RefreshTokenCommand(string RefreshToken) : IRequest<LoginResult>;

public class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, LoginResult>
{
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IUserRepository _userRepository;
    private readonly ITokenService _tokenService;

    public RefreshTokenCommandHandler(
        IRefreshTokenService refreshTokenService,
        IUserRepository userRepository,
        ITokenService tokenService)
    {
        _refreshTokenService = refreshTokenService;
        _userRepository = userRepository;
        _tokenService = tokenService;
    }

    public async Task<LoginResult> HandleAsync(RefreshTokenCommand request, CancellationToken cancellationToken = default)
    {
        var info = await _refreshTokenService.ValidateAsync(request.RefreshToken, cancellationToken);
        if (info is null)
            throw new UnauthorizedAccessException(LocalizationKeys.Auth_InvalidRefreshToken);

        var user = await _userRepository.GetByIdAsync(info.UserId, cancellationToken);
        if (user is null || !user.IsActive || user.TenantId != info.TenantId)
            throw new UnauthorizedAccessException(LocalizationKeys.Auth_InvalidUser);

        await _refreshTokenService.RevokeAsync(request.RefreshToken, cancellationToken);
        var accessToken = _tokenService.GenerateAccessToken(user);
        var newRefresh = await _refreshTokenService.IssueAsync(user.Id, user.TenantId, cancellationToken);

        return new LoginResult(accessToken, newRefresh, DateTime.UtcNow.AddHours(1), false);
    }
}
