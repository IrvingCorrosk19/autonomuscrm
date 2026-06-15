using AutonomusCRM.AI;
using AutonomusCRM.Application;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.Platform;
using AutonomusCRM.Workers;
using AutonomusCRM.Workers.Agents;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        ProductionConfigurationGuard.Validate(context.HostingEnvironment, context.Configuration);

        services.AddApplication();
        services.AddInfrastructure(context.Configuration);
        services.AddScoped<ICurrentTenantAccessor, WorkerTenantAccessor>();
        services.AddScoped<IAgentConfigurationService, WorkerAgentConfigurationService>();
        services.AddPlatformOpenTelemetry(context.Configuration, "AutonomusCRM.Workers");
        services.AddAiRuntime(context.Configuration);

        // Register Agents
        services.AddScoped<LeadIntelligenceAgent>();
        services.AddScoped<CustomerRiskAgent>();
        services.AddScoped<DealStrategyAgent>();
        services.AddScoped<CommunicationAgent>();
        services.AddScoped<CustomerHealthAgent>();
        services.AddScoped<ChurnRiskAgent>();
        services.AddScoped<RenewalAgent>();
        services.AddScoped<ExpansionAgent>();
        services.AddScoped<CustomerInsightsAgent>();
        services.AddScoped<DataQualityGuardian>();
        services.AddScoped<ComplianceSecurityAgent>();
        services.AddScoped<AutomationOptimizerAgent>();

        // Register Worker
        services.AddHostedService<Worker>();
        services.AddHostedService<BusinessMemoryConsolidationWorker>();

        var dataHubMode = context.Configuration.GetValue<DataHubProcessingMode>(
            "DataHub:ProcessingMode", DataHubProcessingMode.InProcess);
        if (dataHubMode == DataHubProcessingMode.RabbitMQ)
            services.AddHostedService<DataHubImportRabbitWorker>();
        services.AddHostedService<DataHubOrphanRecoveryWorker>();
    })
    .Build();

await host.RunAsync();
