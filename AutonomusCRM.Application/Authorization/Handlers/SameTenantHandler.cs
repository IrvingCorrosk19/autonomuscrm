using AutonomusCRM.Application.Authorization.Policies;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace AutonomusCRM.Application.Authorization.Handlers;

public class SameTenantRequirement : IAuthorizationRequirement
{
}

public class SameTenantHandler : AuthorizationHandler<SameTenantRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantRequirement requirement)
    {
        var userTenantId = context.User.FindFirst("TenantId")?.Value;
        
        // TODO: Obtener TenantId del recurso solicitado y comparar
        // Por ahora, si el usuario tiene TenantId, se considera válido
        
        if (!string.IsNullOrEmpty(userTenantId))
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}

