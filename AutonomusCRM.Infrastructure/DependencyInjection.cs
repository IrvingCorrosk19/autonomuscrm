using AutonomusCRM.Application.Auth;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Infrastructure.Auth;
using AutonomusCRM.Infrastructure.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Infrastructure.Persistence.EventStore;
using AutonomusCRM.Infrastructure.Persistence.Repositories;
using AutonomusCRM.Infrastructure.Platform;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddPlatformTenancy();

        // Database
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        var dataSourceBuilder = new Npgsql.NpgsqlDataSourceBuilder(connectionString);
        dataSourceBuilder.EnableDynamicJson();
        var dataSource = dataSourceBuilder.Build();

        services.AddDbContext<ApplicationDbContext>(options =>
            PlatformExtensions.UsePlatformNpgsql(options, dataSource));

        // Repositories
        services.AddScoped<ITenantRepository, TenantRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<ILeadRepository, LeadRepository>();
        services.AddScoped<IDealRepository, DealRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IWorkflowRepository, WorkflowRepository>();
        services.AddScoped<IWorkflowTaskRepository, WorkflowTaskRepository>();
        services.AddScoped<IPolicyRepository, PolicyRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Event Store
        services.AddScoped<Application.Events.EventSourcing.IEventStore, EventStore>();
        services.AddScoped<Application.Events.EventSourcing.ISnapshotStore, SnapshotStore>();

        // Event Dispatcher
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();

        // Automation
        services.AddScoped<Application.Automation.Workflows.IWorkflowEngine, Infrastructure.Automation.WorkflowEngine>();
        services.AddScoped<IAgentConfigurationService, Agents.AgentConfigurationService>();
        services.AddScoped<IOperationalTaskService, Infrastructure.Automation.OperationalTaskService>();
        services.AddScoped<IOperationalAutomationService, Infrastructure.Automation.OperationalAutomationService>();

        services.AddScoped<ISalesQuotaRepository, SalesQuotaRepository>();
        services.AddScoped<IRevenueForecastEngine, Infrastructure.Revenue.RevenueForecastEngine>();
        services.AddScoped<ISalesPerformanceEngine, Infrastructure.Revenue.SalesPerformanceEngine>();
        services.AddScoped<IPipelineCoverageService, Infrastructure.Revenue.PipelineCoverageService>();
        services.AddScoped<IWinLossAnalyticsService, Infrastructure.Revenue.WinLossAnalyticsService>();
        services.AddScoped<ISalesProductivityService, Infrastructure.Revenue.SalesProductivityService>();
        services.AddScoped<ICommercialSlaEngine, Infrastructure.Revenue.CommercialSlaEngine>();
        services.AddScoped<ISmartAssignmentEngine, Infrastructure.Revenue.SmartAssignmentEngine>();
        services.AddScoped<IRevenueAutomationEngine, Infrastructure.Revenue.RevenueAutomationEngine>();
        services.AddScoped<IRevenueKpiService, Infrastructure.Revenue.RevenueKpiService>();
        services.AddScoped<IExecutiveSalesDashboardService, Infrastructure.Revenue.ExecutiveSalesDashboardService>();
        services.AddScoped<ISalesIntelligenceService, Infrastructure.Revenue.SalesIntelligenceService>();
        services.AddScoped<IDataQualityRevenueService, Infrastructure.Revenue.DataQualityRevenueService>();

        services.AddScoped<ICustomerContractRepository, CustomerContractRepository>();
        services.AddScoped<ICustomerCommunicationRepository, CustomerCommunicationRepository>();
        services.AddScoped<ICustomerHealthEngine, Infrastructure.CustomerSuccess.CustomerHealthEngine>();
        services.AddScoped<IChurnRiskEngine, Infrastructure.CustomerSuccess.ChurnRiskEngine>();
        services.AddScoped<IRenewalEngine, Infrastructure.CustomerSuccess.RenewalEngine>();
        services.AddScoped<ICustomerPlaybookService, Infrastructure.CustomerSuccess.CustomerPlaybookService>();
        services.AddScoped<IEmailDeliveryProvider, Infrastructure.CustomerSuccess.LogEmailDeliveryProvider>();
        services.AddScoped<IWhatsAppDeliveryProvider, Infrastructure.CustomerSuccess.LogWhatsAppDeliveryProvider>();
        services.AddScoped<IEmailAutomationEngine, Infrastructure.CustomerSuccess.EmailAutomationEngine>();
        services.AddScoped<IWhatsAppAutomationEngine, Infrastructure.CustomerSuccess.WhatsAppAutomationEngine>();
        services.AddScoped<ICustomerJourneyEngine, Infrastructure.CustomerSuccess.CustomerJourneyEngine>();
        services.AddScoped<IExpansionRevenueEngine, Infrastructure.CustomerSuccess.ExpansionRevenueEngine>();
        services.AddScoped<ICustomerSuccessIntelligenceService, Infrastructure.CustomerSuccess.CustomerSuccessIntelligenceService>();
        services.AddScoped<IRetentionAutomationEngine, Infrastructure.CustomerSuccess.RetentionAutomationEngine>();
        services.AddScoped<ICustomerKpiService, Infrastructure.CustomerSuccess.CustomerKpiService>();
        services.AddScoped<IExecutiveCustomerDashboardService, Infrastructure.CustomerSuccess.ExecutiveCustomerDashboardService>();

        // Decision Engine
        services.AddScoped<Application.DecisionEngine.IDecisionEngine, Infrastructure.DecisionEngine.DecisionEngine>();

        // Policy Engine
        services.AddScoped<Application.Policies.IPolicyEngine, Infrastructure.Policies.PolicyEngine>();

        // Cache (Redis)
        var redisConnectionString = configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options =>
            {
                options.Configuration = redisConnectionString;
            });
            services.AddScoped<Caching.RedisCacheService>();
            services.AddScoped<Caching.ICacheService>(sp => new Caching.TenantScopedCacheService(
                sp.GetRequiredService<Caching.RedisCacheService>(),
                sp.GetRequiredService<Application.Common.Tenancy.ICurrentTenantAccessor>()));
        }
        else
        {
            services.AddMemoryCache();
            services.AddScoped<Caching.MemoryCacheService>();
            services.AddScoped<Caching.ICacheService>(sp => new Caching.TenantScopedCacheService(
                sp.GetRequiredService<Caching.MemoryCacheService>(),
                sp.GetRequiredService<Application.Common.Tenancy.ICurrentTenantAccessor>()));
        }

        services.AddScoped<IRefreshTokenService, RefreshTokenService>();

        // Event Bus (RabbitMQ o InMemory)
        var eventBusProvider = configuration["EventBus:Provider"] ?? "RabbitMQ";
        var rabbitMQOptions = configuration.GetSection("RabbitMQ").Get<Events.EventBus.RabbitMQOptions>();
        if (!eventBusProvider.Equals("InMemory", StringComparison.OrdinalIgnoreCase)
            && rabbitMQOptions != null
            && !string.IsNullOrEmpty(rabbitMQOptions.HostName))
        {
            services.Configure<Events.EventBus.RabbitMQOptions>(options =>
            {
                options.HostName = rabbitMQOptions.HostName;
                options.Port = rabbitMQOptions.Port;
                options.UserName = rabbitMQOptions.UserName;
                options.Password = rabbitMQOptions.Password;
                options.VirtualHost = rabbitMQOptions.VirtualHost;
                options.ExchangeName = rabbitMQOptions.ExchangeName;
                options.QueuePrefix = rabbitMQOptions.QueuePrefix;
            });
            services.AddSingleton<Events.EventBus.IEventBus, Events.EventBus.ResilientRabbitMQEventBus>();
        }
        else
        {
            // Fallback a InMemoryEventBus si RabbitMQ no está configurado
            services.AddSingleton<Events.EventBus.IEventBus, Events.EventBus.InMemoryEventBus>();
        }

        // Metrics
        services.AddSingleton<Metrics.IMetricsService, Metrics.MetricsService>();

        // Time Series
        services.AddScoped<Persistence.TimeSeries.ITimeSeriesRepository, Persistence.TimeSeries.TimeSeriesRepository>();

        // Snapshot Store (ya registrado arriba con Application.Events.EventSourcing.ISnapshotStore)

        // Event Sourcing Service
        services.AddScoped<Application.Events.EventSourcing.IEventSourcingService, Application.Events.EventSourcing.EventSourcingService>();

        // Multi-Region Service
        services.AddScoped<Application.MultiRegion.IRegionService, Application.MultiRegion.RegionService>();
        services.AddScoped<Application.Tenancy.ITenantProvisioningService, Tenancy.TenantProvisioningService>();

        return services;
    }
}

