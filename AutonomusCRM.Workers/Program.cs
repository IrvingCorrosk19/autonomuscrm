using AutonomusCRM.Infrastructure;
using AutonomusCRM.Workers;
using AutonomusCRM.Workers.Agents;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Add Infrastructure
        services.AddInfrastructure(context.Configuration);

        // Register Agents
        services.AddScoped<LeadIntelligenceAgent>();
        services.AddScoped<CustomerRiskAgent>();
        services.AddScoped<DealStrategyAgent>();
        services.AddScoped<CommunicationAgent>();
        services.AddScoped<DataQualityGuardian>();
        services.AddScoped<ComplianceSecurityAgent>();
        services.AddScoped<AutomationOptimizerAgent>();

        // Register Worker
        services.AddHostedService<Worker>();
    })
    .Build();

await host.RunAsync();
