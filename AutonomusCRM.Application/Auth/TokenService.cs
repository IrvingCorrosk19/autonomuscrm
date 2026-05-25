using AutonomusCRM.Domain.Users;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace AutonomusCRM.Application.Auth;

public class TokenService : ITokenService
{
    private readonly IConfiguration _configuration;

    public TokenService(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public string GenerateAccessToken(User user, TimeSpan? lifetime = null)
    {
        var key = GetSigningKey();
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.Add(lifetime ?? TimeSpan.FromHours(1));

        var claims = BuildClaims(user, mfaPending: false);
        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: expires,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateMfaPendingToken(User user)
    {
        var key = GetSigningKey();
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = BuildClaims(user, mfaPending: true);

        var token = new JwtSecurityToken(
            issuer: _configuration["Jwt:Issuer"],
            audience: _configuration["Jwt:Audience"],
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(5),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal CreatePrincipal(User user, string authenticationScheme)
    {
        var identity = new ClaimsIdentity(BuildClaims(user, mfaPending: false), authenticationScheme);
        return new ClaimsPrincipal(identity);
    }

    private List<Claim> BuildClaims(User user, bool mfaPending)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim()),
            new("TenantId", user.TenantId.ToString())
        };

        if (mfaPending)
            claims.Add(new Claim("MfaPending", "true"));

        foreach (var role in user.Roles)
            claims.Add(new Claim(ClaimTypes.Role, role));

        return claims;
    }

    private SymmetricSecurityKey GetSigningKey()
    {
        var jwtKey = _configuration["Jwt:Key"]
            ?? throw new InvalidOperationException("JWT Key not configured");
        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
    }
}
