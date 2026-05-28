using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Infrastructure.Tenancy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace AutonomusCRM.Infrastructure.Platform;

public static class PlatformExtensions
{
    public static IServiceCollection AddPlatformTenancy(this IServiceCollection services)
    {
        services.AddScoped<ICurrentTenantAccessor, CurrentTenantAccessor>();
        return services;
    }

    public static IServiceCollection AddPlatformOpenTelemetry(
        this IServiceCollection services,
        IConfiguration configuration,
        string serviceName)
    {
        var otlpEndpoint = configuration["OpenTelemetry:OtlpEndpoint"];
        var enableConsole = configuration.GetValue("OpenTelemetry:EnableConsoleExporter", true);

        services.AddOpenTelemetry()
            .ConfigureResource(r => r.AddService(
                serviceName: serviceName,
                serviceVersion: typeof(PlatformExtensions).Assembly.GetName().Version?.ToString() ?? "1.0.0"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation(o =>
                    {
                        o.RecordException = true;
                        o.Filter = ctx => !ctx.Request.Path.StartsWithSegments("/health");
                    })
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation(o =>
                    {
                        o.SetDbStatementForText = true;
                        o.SetDbStatementForStoredProcedure = true;
                    })
                    .AddSource("AutonomusCRM.EventBus")
                    .AddSource("AutonomusCRM.Workers");

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    tracing.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));

                if (enableConsole)
                    tracing.AddConsoleExporter();
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (!string.IsNullOrWhiteSpace(otlpEndpoint))
                    metrics.AddOtlpExporter(o => o.Endpoint = new Uri(otlpEndpoint));

                if (enableConsole)
                    metrics.AddConsoleExporter();
            });

        return services;
    }

    public static DbContextOptionsBuilder UsePlatformNpgsql(
        this DbContextOptionsBuilder options,
        NpgsqlDataSource dataSource)
    {
        return options.UseNpgsql(dataSource, npgsql =>
        {
            npgsql.EnableRetryOnFailure(
                maxRetryCount: 5,
                maxRetryDelay: TimeSpan.FromSeconds(15),
                errorCodesToAdd: null);
            npgsql.CommandTimeout(30);
        });
    }
}
