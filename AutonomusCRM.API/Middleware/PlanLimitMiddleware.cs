using AutonomusCRM.Application.Billing;
using AutonomusCRM.Application.Common.Tenancy;

namespace AutonomusCRM.API.Middleware;

public sealed class PlanLimitMiddleware
{
    private readonly RequestDelegate _next;

    public PlanLimitMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context, ICurrentTenantAccessor tenantAccessor, IPlanLimitService limits)
    {
        if (tenantAccessor.TenantId == null || !HttpMethods.IsPost(context.Request.Method))
        {
            await _next(context);
            return;
        }

        var path = context.Request.Path.Value ?? "";
        if (path.StartsWith("/api/billing", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/auth", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/webhooks", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/api/enterprise/scim", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("/Account", StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }

        var tenantId = tenantAccessor.TenantId.Value;
        await limits.RecordApiCallAsync(tenantId, context.RequestAborted);
        var apiCheck = await limits.CheckAsync(tenantId, "api_calls", context.RequestAborted);
        if (!apiCheck.Allowed)
        {
            await WriteLimitResponse(context, apiCheck);
            return;
        }

        var resource = MapResource(path);
        if (resource != null)
        {
            var check = await limits.CheckAsync(tenantId, resource, context.RequestAborted);
            if (!check.Allowed)
            {
                await WriteLimitResponse(context, check);
                return;
            }
        }

        await _next(context);
    }

    private static string? MapResource(string path)
    {
        if (path.Contains("/customers", StringComparison.OrdinalIgnoreCase) || path.Contains("/Customers/Create", StringComparison.OrdinalIgnoreCase))
            return "customers";
        if (path.Contains("/leads", StringComparison.OrdinalIgnoreCase) || path.Contains("/Leads/Create", StringComparison.OrdinalIgnoreCase))
            return "leads";
        if (path.Contains("/deals", StringComparison.OrdinalIgnoreCase) || path.Contains("/Deals/Create", StringComparison.OrdinalIgnoreCase))
            return "deals";
        if (path.Contains("/users", StringComparison.OrdinalIgnoreCase) || path.Contains("/Users/Create", StringComparison.OrdinalIgnoreCase))
            return "users";
        if (path.Contains("/integrations/connect", StringComparison.OrdinalIgnoreCase))
            return "integrations";
        return null;
    }

    private static async Task WriteLimitResponse(HttpContext context, PlanLimitCheckResult check)
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        await context.Response.WriteAsJsonAsync(new
        {
            error = check.Message,
            code = check.Code,
            current = check.Current,
            limit = check.Limit
        });
    }
}
