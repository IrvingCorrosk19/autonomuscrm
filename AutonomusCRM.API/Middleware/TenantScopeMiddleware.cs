using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.API.Middleware;

namespace AutonomusCRM.API.Middleware;

/// <summary>
/// Sincroniza ICurrentTenantAccessor con el usuario autenticado y correlation id.
/// </summary>
public class TenantScopeMiddleware
{
    private readonly RequestDelegate _next;

    public TenantScopeMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentTenantAccessor tenantAccessor)
    {
        if (context.Items.TryGetValue(CorrelationIdMiddleware.HeaderName, out var corr) && corr is string cid)
            tenantAccessor.CorrelationId = cid;

        if (context.User.Identity?.IsAuthenticated == true)
        {
            var claim = context.User.FindFirst("TenantId")?.Value;
            if (Guid.TryParse(claim, out var tenantId))
                tenantAccessor.TenantId = tenantId;
        }

        await _next(context);
    }
}
