using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using BCrypt.Net;

namespace AutonomusCRM.Application.Auth.Commands;

public class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<LoginResult> HandleAsync(LoginCommand request, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(request.TenantId, request.Email, cancellationToken);

        if (user == null || !user.IsActive)
            throw new UnauthorizedAccessException("Credenciales inválidas");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Credenciales inválidas");

        user.RecordLogin();
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (user.MfaEnabled)
        {
            var tempToken = _tokenService.GenerateMfaPendingToken(user);
            return new LoginResult(tempToken, string.Empty, DateTime.UtcNow.AddMinutes(5), true);
        }

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _refreshTokenService.IssueAsync(user.Id, user.TenantId, cancellationToken);

        return new LoginResult(accessToken, refreshToken, DateTime.UtcNow.AddHours(1), false);
    }
}
