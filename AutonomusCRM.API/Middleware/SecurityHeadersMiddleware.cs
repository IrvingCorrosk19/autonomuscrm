namespace AutonomusCRM.API.Middleware;

public class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next) => _next = next;

    public async Task InvokeAsync(HttpContext context)
    {
        context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
        context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
        context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
        context.Response.Headers.TryAdd("X-XSS-Protection", "0");
        context.Response.Headers.TryAdd("Permissions-Policy", "geolocation=(), microphone=(), camera=()");
        context.Response.Headers.TryAdd("Content-Security-Policy",
            "default-src 'self'; script-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net; style-src 'self' 'unsafe-inline' https://cdn.jsdelivr.net https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com https://cdn.jsdelivr.net; img-src 'self' data:; connect-src 'self'; frame-ancestors 'none'; base-uri 'self'; form-action 'self'");

        if (context.Request.IsHttps)
            context.Response.Headers.TryAdd("Strict-Transport-Security", "max-age=31536000; includeSubDomains");

        await _next(context);
    }
}
