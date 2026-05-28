using AutonomusCRM.Application.Authorization.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace AutonomusCRM.Application.Authorization.Handlers;

public class SameTenantRequirement : IAuthorizationRequirement
{
}

public class SameTenantHandler : AuthorizationHandler<SameTenantRequirement>
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SameTenantHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameTenantRequirement requirement)
    {
        var userTenantClaim = context.User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(userTenantClaim) || !Guid.TryParse(userTenantClaim, out var userTenantId))
            return Task.CompletedTask;

        var httpContext = _httpContextAccessor.HttpContext;
        if (httpContext is null)
        {
            context.Succeed(requirement);
            return Task.CompletedTask;
        }

        if (TryGetRequestedTenantId(httpContext, out var requestedTenantId))
        {
            if (requestedTenantId == userTenantId)
                context.Succeed(requirement);
            return Task.CompletedTask;
        }

        // Sin tenant explícito en la petición: solo exigir claim válido (páginas UI por claim).
        context.Succeed(requirement);
        return Task.CompletedTask;
    }

    private static bool TryGetRequestedTenantId(HttpContext httpContext, out Guid tenantId)
    {
        tenantId = default;
        if (httpContext.Request.Query.TryGetValue("tenantId", out var q) &&
            Guid.TryParse(q.ToString(), out tenantId))
            return true;

        if (httpContext.Request.RouteValues.TryGetValue("tenantId", out var rv) &&
            Guid.TryParse(rv?.ToString(), out tenantId))
            return true;

        return false;
    }
}

