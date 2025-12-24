using AutonomusCRM.Application;
using AutonomusCRM.Application.Authorization;
using AutonomusCRM.Infrastructure;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

// Configurar Serilog para logging estructurado
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/autonomuscrm-.txt", rollingInterval: RollingInterval.Day)
    .CreateLogger();

try
{
    Log.Information("Starting AUTONOMUS CRM API");

    var builder = WebApplication.CreateBuilder(args);

    // Usar Serilog
    builder.Host.UseSerilog();

    // Add services to the container
    builder.Services.AddControllers();
    builder.Services.AddRazorPages();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new() { 
            Title = "AUTONOMUS CRM API", 
            Version = "v1",
            Description = "El Sistema de Gestión Empresarial Autónomo más avanzado jamás concebido"
        });
    });

    // Add Application
    builder.Services.AddApplication();

    // Add Infrastructure
    builder.Services.AddInfrastructure(builder.Configuration);

    // Health Checks
    builder.Services.AddHealthChecks()
        .AddCheck<AutonomusCRM.Infrastructure.Health.DatabaseHealthCheck>("database", tags: new[] { "db", "postgresql" })
        .AddCheck<AutonomusCRM.Infrastructure.Health.EventBusHealthCheck>("eventbus", tags: new[] { "eventbus", "rabbitmq" })
        .AddCheck<AutonomusCRM.Infrastructure.Health.CacheHealthCheck>("cache", tags: new[] { "cache", "redis" });

    // JWT Authentication
    var jwtKey = builder.Configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT Key not configured");
    var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? throw new InvalidOperationException("JWT Issuer not configured");
    var jwtAudience = builder.Configuration["Jwt:Audience"] ?? throw new InvalidOperationException("JWT Audience not configured");

    builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "Bearer";
        options.DefaultChallengeScheme = "Bearer";
    })
    .AddJwtBearer("Bearer", options =>
    {
        options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(
                System.Text.Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };
    });

    // Authorization
    builder.Services.AddAuthorization(options =>
    {
        options.AddAutonomusPolicies();
    });

    builder.Services.AddScoped<Microsoft.AspNetCore.Authorization.IAuthorizationHandler, 
        AutonomusCRM.Application.Authorization.Handlers.SameTenantHandler>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapRazorPages();
app.MapControllers();

// Health Checks endpoint
app.MapHealthChecks("/health");
app.MapHealthChecks("/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});
app.MapHealthChecks("/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    Predicate = _ => false
});

// NOTA: Las migraciones se aplican manualmente con:
// dotnet ef database update --project AutonomusCRM.Infrastructure
// NO usar Database.Migrate() aquí para evitar problemas con EF Core Design-Time

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
