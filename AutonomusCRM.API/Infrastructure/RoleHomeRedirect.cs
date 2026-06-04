using System.Security.Claims;

namespace AutonomusCRM.API.Infrastructure;

public static class RoleHomeRedirect
{
    /// <summary>CEO/Admin → Executive · CRO/Sales → Revenue OS · CCO/Support → Customer360.</summary>
    public static string GetHomePath(ClaimsPrincipal user)
    {
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (roles.Contains("Admin"))
            return "/executive";
        if (roles.Contains("Manager"))
            return "/executive";
        if (roles.Contains("Sales"))
            return "/revenue";
        if (roles.Contains("Support"))
            return "/Customer360";
        return "/";
    }
}
