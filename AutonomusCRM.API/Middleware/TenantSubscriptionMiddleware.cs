using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.SaaS;

namespace AutonomusCRM.API.Middleware;

/// <summary>Blocks API usage when tenant subscription expired (when SaaS:EnforceSubscription=true).</summary>
public sealed class TenantSubscriptionMiddleware
{
    private readonly RequestDelegate _next;

    public TenantSubscriptionMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentTenantAccessor tenantAccessor, IConfiguration config)
    {
        var enforce = config.GetValue("SaaS:EnforceSubscription", false);
        if (!enforce || tenantAccessor.TenantId == null)
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/provisioning", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var db = context.RequestServices.GetRequiredService<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>();
        var tenant = await db.Tenants.FindAsync(new object[] { tenantAccessor.TenantId.Value }, context.RequestAborted);
        if (tenant?.SubscriptionExpiresAt is { } exp && exp < DateTime.UtcNow)
        {
            context.Response.StatusCode = StatusCodes.Status402PaymentRequired;
            await context.Response.WriteAsJsonAsync(new { error = "Subscription expired", code = "SUBSCRIPTION_EXPIRED" });
            return;
        }

        await _next(context);
    }
}
