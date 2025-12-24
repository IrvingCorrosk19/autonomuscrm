using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.Authorization.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace AutonomusCRM.Application.Authorization;

public static class Extensions
{
    public static AuthorizationOptions AddAutonomusPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(AuthorizationPolicies.RequireAdmin, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(AuthorizationPolicies.RequireManager, policy =>
            policy.RequireRole("Admin", "Manager"));

        options.AddPolicy(AuthorizationPolicies.RequireSales, policy =>
            policy.RequireRole("Admin", "Manager", "Sales"));

        options.AddPolicy(AuthorizationPolicies.RequireSameTenant, policy =>
            policy.Requirements.Add(new SameTenantRequirement()));

        return options;
    }
}

