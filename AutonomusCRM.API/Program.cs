using System.Net;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Npgsql;
using AutonomusCRM.AI;
using AutonomusCRM.API.Extensions;
using AutonomusCRM.API.Resources;
using AutonomusCRM.API.Middleware;
using AutonomusCRM.Application;
using AutonomusCRM.Application.Auth;
using AutonomusCRM.Application.Authorization;
using AutonomusCRM.Infrastructure;
using AutonomusCRM.Infrastructure.Platform;
using AutonomusCRM.Application.EnterpriseAuth;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using Serilog.Events;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/autonomuscrm-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    NpgsqlConnection.GlobalTypeMapper.EnableDynamicJson();
    Log.Information("Starting AUTONOMUS CRM API");

    var builder = WebApplication.CreateBuilder(args);

    ProductionConfigurationGuard.Validate(builder.Environment, builder.Configuration);

    builder.Host.UseSerilog();

    var jwtKey = builder.Configuration["Jwt:Key"]
        ?? throw new InvalidOperationException("JWT Key not configured. Set Jwt__Key environment variable.");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AutonomusCRM";
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AutonomusCRM";

    builder.Services.AddApplication();
    builder.Services.AddAppLocalization();
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<AutonomusCRM.Application.Common.Tenancy.ITenantContext, AutonomusCRM.API.Infrastructure.TenantContext>();
    builder.Services.AddScoped<ITokenService, TokenService>();
    builder.Services.AddInfrastructure(builder.Configuration);
    builder.Services.AddScoped<AutonomusCRM.Application.DataHub.IDataHubRequestContext, AutonomusCRM.API.Infrastructure.DataHubHttpRequestContext>();
    builder.Services.AddSignalR();
    builder.Services.AddScoped<AutonomusCRM.Application.DataHub.IDataHubProgressNotifier, AutonomusCRM.API.Hubs.DataHubProgressNotifier>();
    builder.Services.AddScoped<AutonomusCRM.Application.DatabaseIntelligence.IDbIntelligenceProgressNotifier, AutonomusCRM.API.Hubs.DbIntelligenceProgressNotifier>();
    builder.Services.AddScoped<AutonomusCRM.Application.DatabaseIntelligence.IDbIntelligenceBusinessProgressNotifier, AutonomusCRM.API.Hubs.DbIntelligenceProgressNotifier>();
    builder.Services.AddScoped<AutonomusCRM.Application.DatabaseIntelligence.IDbIntelligenceHealthProgressNotifier, AutonomusCRM.API.Hubs.DbIntelligenceProgressNotifier>();
    builder.Services.AddScoped<AutonomusCRM.Application.DatabaseIntelligence.IDbIntelligenceGraphProgressNotifier, AutonomusCRM.API.Hubs.DbIntelligenceProgressNotifier>();
    builder.Services.AddScoped<AutonomusCRM.Application.DatabaseIntelligence.IDbIntelligenceSyncProgressNotifier, AutonomusCRM.API.Hubs.DbIntelligenceProgressNotifier>();
    builder.Services.AddScoped<AutonomusCRM.Application.DatabaseIntelligence.IDbOperationProgressNotifier, AutonomusCRM.API.Hubs.DbIntelligenceProgressNotifier>();
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/root/.aspnet/DataProtection-Keys"));
    builder.Services.AddPlatformOpenTelemetry(builder.Configuration, "AutonomusCRM.API");
    builder.Services.AddAiRuntime(builder.Configuration);

    builder.Services.AddControllers(options =>
    {
        var policy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .AddAuthenticationSchemes(
                CookieAuthenticationDefaults.AuthenticationScheme,
                JwtBearerDefaults.AuthenticationScheme)
            .Build();
        options.Filters.Add(new AuthorizeFilter(policy));
    })
    .AddDataAnnotationsLocalization(options =>
    {
        options.DataAnnotationLocalizerProvider = (_, factory) =>
            factory.Create(typeof(AutonomusCRM.API.Resources.ValidationMessages));
    });

    builder.Services.AddRazorPages(options =>
    {
        options.Conventions.AuthorizeFolder("/");
        options.Conventions.AllowAnonymousToFolder("/Account");
        options.Conventions.AllowAnonymousToPage("/Error");
    })
    .AddViewLocalization(Microsoft.AspNetCore.Mvc.Razor.LanguageViewLocationExpanderFormat.Suffix);

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "AUTONOMUS CRM API",
            Version = "v1",
            Description = "API REST — Autonomus CRM"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddHealthChecks()
        .AddCheck<AutonomusCRM.Infrastructure.Health.DatabaseHealthCheck>("database", tags: new[] { "db", "ready" })
        .AddCheck<AutonomusCRM.Infrastructure.Health.EventBusHealthCheck>("eventbus", tags: new[] { "ready" })
        .AddCheck<AutonomusCRM.Infrastructure.Health.CacheHealthCheck>("cache", tags: new[] { "ready" });

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Smart";
        options.DefaultChallengeScheme = "Smart";
    })
    .AddPolicyScheme("Smart", "JWT or Cookie", options =>
    {
        options.ForwardDefaultSelector = context =>
        {
            var auth = context.Request.Headers.Authorization.ToString();
            if (!string.IsNullOrEmpty(auth) && auth.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                return JwtBearerDefaults.AuthenticationScheme;
            return CookieAuthenticationDefaults.AuthenticationScheme;
        };
    })
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = builder.Configuration.GetValue("Auth:AllowInsecureCookies", false)
            ? CookieSecurePolicy.SameAsRequest
            : builder.Environment.IsProduction()
                ? CookieSecurePolicy.Always
                : CookieSecurePolicy.SameAsRequest;
        options.Events.OnRedirectToLogin = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect("/Account/Login" + (ctx.Request.QueryString.HasValue ? ctx.Request.QueryString.Value : ""));
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = ctx =>
        {
            if (ctx.Request.Path.StartsWithSegments("/api"))
            {
                ctx.Response.StatusCode = StatusCodes.Status403Forbidden;
                return Task.CompletedTask;
            }
            ctx.Response.Redirect("/Account/AccessDenied");
            return Task.CompletedTask;
        };
    })
    .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    var enterpriseAuth = builder.Configuration.GetSection(EnterpriseAuthOptions.SectionName).Get<EnterpriseAuthOptions>();
    if (enterpriseAuth?.Enabled == true && !string.IsNullOrWhiteSpace(enterpriseAuth.OidcAuthority))
    {
        builder.Services.AddAuthentication()
            .AddOpenIdConnect(OpenIdConnectDefaults.AuthenticationScheme, options =>
            {
                options.Authority = enterpriseAuth.OidcAuthority;
                options.ClientId = enterpriseAuth.OidcClientId;
                options.ClientSecret = enterpriseAuth.OidcClientSecret;
                options.ResponseType = "code";
                options.SaveTokens = true;
                options.Scope.Add("openid");
                options.Scope.Add("profile");
                options.Scope.Add("email");
            });
    }

    builder.Services.AddAuthorization(options => options.AddAutonomusPolicies());
    builder.Services.AddScoped<IAuthorizationHandler, AutonomusCRM.Application.Authorization.Handlers.SameTenantHandler>();

    builder.Services.AddRateLimiter(options =>
    {
        options.AddPolicy("login", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 10,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                }));

        options.AddPolicy("per-tenant-api", httpContext =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolveTenantRateLimitKey(httpContext),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 120,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 5
                }));

        options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: ResolveTenantRateLimitKey(context),
                factory: _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = 200,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                    QueueLimit = 10
                }));
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    });

    builder.Services.AddHsts(options =>
    {
        options.MaxAge = TimeSpan.FromDays(365);
        options.IncludeSubDomains = true;
    });

    var app = builder.Build();

    var forwardedHeaders = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    };
    forwardedHeaders.KnownNetworks.Clear();
    forwardedHeaders.KnownProxies.Clear();
    forwardedHeaders.KnownProxies.Add(IPAddress.Loopback);
    forwardedHeaders.KnownProxies.Add(IPAddress.IPv6Loopback);
    app.UseForwardedHeaders(forwardedHeaders);

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseMiddleware<ExceptionHandlingMiddleware>();
    app.UseMiddleware<SecurityHeadersMiddleware>();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
        app.UseExceptionHandler("/Error");
    }
    else
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Solo redirigir a HTTPS cuando el proceso realmente escucha HTTPS (evita WRN en VPS HTTP-only).
    var appUrls = builder.Configuration["ASPNETCORE_URLS"] ?? Environment.GetEnvironmentVariable("ASPNETCORE_URLS") ?? "";
    if (appUrls.Contains("https://", StringComparison.OrdinalIgnoreCase))
        app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseAppLocalization();
    app.UseRouting();
    app.UseRateLimiter();
    app.UseAuthentication();
    app.UseAuthorization();
    app.UseMiddleware<TenantScopeMiddleware>();
    app.UseMiddleware<TenantSubscriptionMiddleware>();
    app.UseMiddleware<PlanLimitMiddleware>();
    app.UseMiddleware<CommercialWriteAuthorizationMiddleware>();
    app.UseMiddleware<ApiTenantValidationMiddleware>();

    app.MapRazorPages();
    app.MapControllers().RequireRateLimiting("per-tenant-api");
    app.MapHub<AutonomusCRM.API.Hubs.DataHubProgressHub>("/hubs/datahub");
    app.MapHub<AutonomusCRM.API.Hubs.DbIntelligenceProgressHub>("/hubs/db-intelligence");

    app.MapHealthChecks("/health");
    app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains("ready")
    });
    app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
    {
        Predicate = _ => false
    });

    await app.ApplyMigrationsAsync();
    await app.InitializeDatabaseAsync();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static string ResolveTenantRateLimitKey(HttpContext context)
{
    var tenant = context.User.FindFirst("tenant_id")?.Value
        ?? context.User.FindFirst("TenantId")?.Value;
    if (!string.IsNullOrWhiteSpace(tenant))
        return $"tenant:{tenant}";
    if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var header) && !string.IsNullOrWhiteSpace(header))
        return $"tenant:{header.ToString()}";
    return context.User.Identity?.Name ?? context.Connection.RemoteIpAddress?.ToString() ?? "anonymous";
}

public partial class Program { }
