using AutonomusCRM.AI;
using AutonomusCRM.Infrastructure;
using AutonomusCRM.Infrastructure.Platform;
using AutonomusCRM.Workers;
using AutonomusCRM.Workers.Agents;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddInfrastructure(context.Configuration);
        services.AddPlatformOpenTelemetry(context.Configuration, "AutonomusCRM.Workers");
        services.AddAiPlaceholders(context.Configuration);

        // Register Agents
        services.AddScoped<LeadIntelligenceAgent>();
        services.AddScoped<CustomerRiskAgent>();
        services.AddScoped<DealStrategyAgent>();
        services.AddScoped<CommunicationAgent>();
        services.AddScoped<CustomerHealthAgent>();
        services.AddScoped<ChurnRiskAgent>();
        services.AddScoped<RenewalAgent>();
        services.AddScoped<ExpansionAgent>();
        services.AddScoped<DataQualityGuardian>();
        services.AddScoped<ComplianceSecurityAgent>();
        services.AddScoped<AutomationOptimizerAgent>();

        // Register Worker
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
