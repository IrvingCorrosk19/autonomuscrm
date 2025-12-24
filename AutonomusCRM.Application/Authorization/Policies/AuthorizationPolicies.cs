using Microsoft.AspNetCore.Authorization;

namespace AutonomusCRM.Application.Authorization.Policies;

public static class AuthorizationPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireManager = "RequireManager";
    public const string RequireSales = "RequireSales";
    public const string RequireSameTenant = "RequireSameTenant";
}

