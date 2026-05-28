namespace AutonomusCRM.API.Middleware;

/// <summary>
/// Restringe POST de escritura comercial a Admin, Manager y Sales (Viewer/Support solo lectura en Leads/Customers/Deals).
/// </summary>
public class CommercialWriteAuthorizationMiddleware
{
    private static readonly string[] WritePathPrefixes =
    [
        "/Leads",
        "/Customers",
        "/Deals",
        "/Workflows",
        "/Policies"
    ];

    private static readonly string[] WriteRoles = ["Admin", "Manager", "Sales"];

    private readonly RequestDelegate _next;

    public CommercialWriteAuthorizationMiddleware(RequestDelegate next) => _next = next;

    private static readonly string[] WriteOnlyPageSegments = ["/Create", "/Edit"];

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.User.Identity?.IsAuthenticated == true
            && IsCommercialWritePath(context.Request.Path.Value)
            && !WriteRoles.Any(context.User.IsInRole))
        {
            var path = context.Request.Path.Value ?? string.Empty;
            if (HttpMethods.IsPost(context.Request.Method)
                || (HttpMethods.IsGet(context.Request.Method) && WriteOnlyPageSegments.Any(seg =>
                    path.Contains(seg, StringComparison.OrdinalIgnoreCase))))
            {
                context.Response.Redirect("/Account/AccessDenied");
                return;
            }
        }

        await _next(context);
    }

    private static bool IsCommercialWritePath(string? path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return WritePathPrefixes.Any(prefix =>
            path.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
    }
}
