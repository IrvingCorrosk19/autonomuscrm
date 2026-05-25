using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using OtpNet;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AutonomusCRM.Application.Auth.Commands;

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;
    private readonly ITokenService _tokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly ILogger<VerifyMfaCommandHandler> _logger;

    public VerifyMfaCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration,
        ITokenService tokenService,
        IRefreshTokenService refreshTokenService,
        ILogger<VerifyMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
        _tokenService = tokenService;
        _refreshTokenService = refreshTokenService;
        _logger = logger;
    }

    public async Task<LoginResult> HandleAsync(VerifyMfaCommand request, CancellationToken cancellationToken = default)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured"));

        var validationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = true,
            ValidIssuer = _configuration["Jwt:Issuer"],
            ValidateAudience = true,
            ValidAudience = _configuration["Jwt:Audience"],
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        var principal = tokenHandler.ValidateToken(request.TempToken, validationParameters, out _);
        var mfaPending = principal.FindFirst("MfaPending")?.Value;
        if (mfaPending != "true")
            throw new UnauthorizedAccessException("Token MFA inválido");

        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Token inválido");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
            throw new UnauthorizedAccessException("Usuario no tiene MFA habilitado");

        var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecret));
        if (!totp.VerifyTotp(request.MfaCode, out _, new VerificationWindow(1, 1)))
            throw new UnauthorizedAccessException("Código MFA inválido");

        var accessToken = _tokenService.GenerateAccessToken(user);
        var refreshToken = await _refreshTokenService.IssueAsync(user.Id, user.TenantId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new LoginResult(accessToken, refreshToken, DateTime.UtcNow.AddHours(1), false);
    }
}
