using AutonomusCRM.Domain.Users;
using System.Security.Claims;

namespace AutonomusCRM.Application.Auth;

public interface ITokenService
{
    string GenerateAccessToken(User user, TimeSpan? lifetime = null);
    string GenerateMfaPendingToken(User user);
    ClaimsPrincipal CreatePrincipal(User user, string authenticationScheme);
}
