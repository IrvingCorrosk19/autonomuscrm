using System.Text.Json;

namespace AutonomusCRM.API.Middleware;

/// <summary>
/// Impide que un JWT de un tenant consulte o envíe otro tenantId en query/body (API).
/// </summary>
public class ApiTenantValidationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiTenantValidationMiddleware> _logger;

    public ApiTenantValidationMiddleware(RequestDelegate next, ILogger<ApiTenantValidationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!context.Request.Path.StartsWithSegments("/api", StringComparison.OrdinalIgnoreCase)
            || context.User.Identity?.IsAuthenticated != true)
        {
            await _next(context);
            return;
        }

        var userTenantClaim = context.User.FindFirst("TenantId")?.Value;
        if (string.IsNullOrEmpty(userTenantClaim) || !Guid.TryParse(userTenantClaim, out var userTenantId))
        {
            await _next(context);
            return;
        }

        if (context.Request.Query.TryGetValue("tenantId", out var queryTenant)
            && Guid.TryParse(queryTenant.ToString(), out var requestedTenantId)
            && requestedTenantId != userTenantId)
        {
            _logger.LogWarning("API tenant mismatch query: user={UserTenant} requested={Requested}", userTenantId, requestedTenantId);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsJsonAsync(new { error = "TenantId no coincide con el token." });
            return;
        }

        if (HttpMethods.IsPost(context.Request.Method) || HttpMethods.IsPut(context.Request.Method))
        {
            context.Request.EnableBuffering();
            try
            {
                using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
                var body = await reader.ReadToEndAsync();
                context.Request.Body.Position = 0;
                if (!string.IsNullOrWhiteSpace(body))
                {
                    using var doc = JsonDocument.Parse(body);
                    if (doc.RootElement.TryGetProperty("tenantId", out var bodyTenant)
                        && bodyTenant.TryGetGuid(out var bodyTenantId)
                        && bodyTenantId != userTenantId)
                    {
                        _logger.LogWarning("API tenant mismatch body: user={UserTenant} requested={Requested}", userTenantId, bodyTenantId);
                        context.Response.StatusCode = StatusCodes.Status403Forbidden;
                        await context.Response.WriteAsJsonAsync(new { error = "TenantId no coincide con el token." });
                        return;
                    }
                }
            }
            catch (JsonException)
            {
                context.Request.Body.Position = 0;
            }
        }

        await _next(context);
    }
}
