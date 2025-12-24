using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.Authorization.Handlers;
using Microsoft.AspNetCore.Authorization;

namespace AutonomusCRM.Application.Authorization;

public static class Extensions
{
    public static AuthorizationOptions AddAutonomusPolicies(this AuthorizationOptions options)
    {
        options.AddPolicy(Policies.AuthorizationPolicies.RequireAdmin, policy =>
            policy.RequireRole("Admin"));

        options.AddPolicy(Policies.AuthorizationPolicies.RequireManager, policy =>
            policy.RequireRole("Admin", "Manager"));

        options.AddPolicy(Policies.AuthorizationPolicies.RequireSales, policy =>
            policy.RequireRole("Admin", "Manager", "Sales"));

        options.AddPolicy(Policies.AuthorizationPolicies.RequireSameTenant, policy =>
            policy.Requirements.Add(new SameTenantRequirement()));

        return options;
    }
}

