using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AutonomusCRM.Infrastructure.Platform;

/// <summary>
/// Fail-fast validation for Staging/Production configuration.
/// </summary>
public static class ProductionConfigurationGuard
{
    public static void Validate(IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsProduction() && !environment.IsStaging())
            return;

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(configuration.GetConnectionString("DefaultConnection")))
            errors.Add("ConnectionStrings:DefaultConnection is required in Staging/Production.");

        var jwtKey = configuration["Jwt:Key"];
        if (string.IsNullOrWhiteSpace(jwtKey) || jwtKey.Length < 32)
            errors.Add("Jwt:Key must be at least 32 characters in Staging/Production.");

        if (string.IsNullOrWhiteSpace(configuration["IntegrationEncryption:Key"]))
            errors.Add("IntegrationEncryption:Key is required in Staging/Production (base64, 32+ bytes).");

        var eventBusProvider = configuration["EventBus:Provider"] ?? "RabbitMQ";
        var rabbitHost = configuration["RabbitMQ:HostName"];
        if (eventBusProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase))
            errors.Add("EventBus:Provider=InMemory is not allowed in Staging/Production.");

        if (string.IsNullOrWhiteSpace(rabbitHost))
            errors.Add("RabbitMQ:HostName is required in Staging/Production.");

        if (environment.IsProduction() && string.IsNullOrWhiteSpace(configuration.GetConnectionString("Redis")))
            errors.Add("ConnectionStrings:Redis is required in Production (MemoryCache fallback not allowed).");

        var comms = configuration.GetSection(CommunicationOptions.SectionName).Get<CommunicationOptions>()
                    ?? new CommunicationOptions();
        if (!comms.AllowSimulation)
        {
            if (comms.EmailProvider.Equals("Log", StringComparison.OrdinalIgnoreCase))
                errors.Add("Communications:EmailProvider=Log is not allowed when AllowSimulation=false.");
            if (comms.WhatsAppProvider.Equals("Log", StringComparison.OrdinalIgnoreCase))
                errors.Add("Communications:WhatsAppProvider=Log is not allowed when AllowSimulation=false.");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(
                "Production configuration validation failed:\n- " + string.Join("\n- ", errors));
        }
    }
}
