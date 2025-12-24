using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using OtpNet;

namespace AutonomusCRM.Application.Auth.Commands;

public class VerifyMfaCommandHandler : IRequestHandler<VerifyMfaCommand, LoginResult>
{
    private readonly IUserRepository _userRepository;
    private readonly IConfiguration _configuration;
    private readonly ILogger<VerifyMfaCommandHandler> _logger;

    public VerifyMfaCommandHandler(
        IUserRepository userRepository,
        IConfiguration configuration,
        ILogger<VerifyMfaCommandHandler> logger)
    {
        _userRepository = userRepository;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<LoginResult> HandleAsync(VerifyMfaCommand request, CancellationToken cancellationToken = default)
    {
        // Validar temp token
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

        var principal = tokenHandler.ValidateToken(request.TempToken, validationParameters, out var validatedToken);
        var userIdClaim = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        
        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            throw new UnauthorizedAccessException("Token inválido");

        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.MfaEnabled || string.IsNullOrEmpty(user.MfaSecret))
            throw new UnauthorizedAccessException("Usuario no tiene MFA habilitado");

        // Validar código MFA
        var totp = new Totp(Base32Encoding.ToBytes(user.MfaSecret));
        if (!totp.VerifyTotp(request.MfaCode, out _, new VerificationWindow(1, 1)))
            throw new UnauthorizedAccessException("Código MFA inválido");

        // Generar tokens finales
        var accessToken = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        return new LoginResult(accessToken, refreshToken, DateTime.UtcNow.AddHours(1), false);
    }

    private string GenerateJwtToken(Domain.Users.User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
            _configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured")));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new("TenantId", user.TenantId.ToString()),
        };

        foreach (var role in user.Roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Guid.NewGuid().ToString();
    }
}

