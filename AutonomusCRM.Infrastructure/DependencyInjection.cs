using AutonomusCRM.Application.Auth;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;
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

        services.Configure<Application.SaaS.SaasPlanOptions>(
            configuration.GetSection(Application.SaaS.SaasPlanOptions.SectionName));

        services.Configure<AutonomousPlatformOptions>(options =>
        {
            configuration.GetSection(AutonomousPlatformOptions.SectionName).Bind(options);
            var aiEnabled = configuration.GetValue<bool?>("AI:Enabled")
                ?? configuration.GetValue<bool?>("AI__Enabled");
            if (aiEnabled.HasValue)
                options.Enabled = aiEnabled.Value;
        });
        services.AddScoped<Infrastructure.Autonomous.IAutonomousPlatformGate, Infrastructure.Autonomous.AutonomousPlatformGate>();

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
        services.Configure<Application.CustomerSuccess.CommunicationOptions>(
            configuration.GetSection(Application.CustomerSuccess.CommunicationOptions.SectionName));
        services.Configure<Billing.StripeBillingOptions>(configuration.GetSection(Billing.StripeBillingOptions.SectionName));
        services.Configure<Application.EnterpriseAuth.EnterpriseAuthOptions>(
            configuration.GetSection(Application.EnterpriseAuth.EnterpriseAuthOptions.SectionName));
        services.Configure<Application.Integrations.IntegrationOAuthOptions>(
            configuration.GetSection(Application.Integrations.IntegrationOAuthOptions.SectionName));
        services.AddHttpClient();
        services.AddMemoryCache();
        services.AddSingleton<Application.CustomerSuccess.ICommunicationStatusService, CustomerSuccess.CommunicationStatusService>();
        services.AddScoped<Application.Billing.IPlanLimitService, Billing.PlanLimitService>();
        services.AddScoped<Application.Integrations.IIntegrationOAuthService, Integrations.IntegrationOAuthService>();
        services.AddScoped<Application.EnterpriseAuth.IScimUserService, EnterpriseAuth.ScimUserService>();
        services.AddScoped<Application.Voice.IVoiceCallLogRepository, Voice.VoiceCallLogRepository>();
        services.AddScoped<Application.Voice.IVoiceCallService, Voice.VoiceCallService>();
        services.AddScoped<Autonomous.IAutonomousDecisionExecutor, Autonomous.AutonomousDecisionExecutor>();
        services.AddScoped<Application.CustomerSuccess.IEmailDeliveryProvider>(sp =>
        {
            var comm = configuration.GetSection(Application.CustomerSuccess.CommunicationOptions.SectionName)
                .Get<Application.CustomerSuccess.CommunicationOptions>();
            return (comm?.EmailProvider ?? "Log").ToLowerInvariant() switch
            {
                "smtp" => sp.GetRequiredService<Infrastructure.CustomerSuccess.SmtpEmailDeliveryProvider>(),
                "sendgrid" => sp.GetRequiredService<Infrastructure.CustomerSuccess.SendGridEmailDeliveryProvider>(),
                "ses" => sp.GetRequiredService<Infrastructure.CustomerSuccess.SesEmailDeliveryProvider>(),
                _ => sp.GetRequiredService<Infrastructure.CustomerSuccess.LogEmailDeliveryProvider>()
            };
        });
        services.AddScoped<Infrastructure.CustomerSuccess.LogEmailDeliveryProvider>();
        services.AddScoped<Infrastructure.CustomerSuccess.SmtpEmailDeliveryProvider>();
        services.AddScoped<Infrastructure.CustomerSuccess.SendGridEmailDeliveryProvider>();
        services.AddScoped<Infrastructure.CustomerSuccess.SesEmailDeliveryProvider>();
        services.AddScoped<Application.CustomerSuccess.IWhatsAppDeliveryProvider>(sp =>
        {
            var comm = configuration.GetSection(Application.CustomerSuccess.CommunicationOptions.SectionName)
                .Get<Application.CustomerSuccess.CommunicationOptions>();
            return string.Equals(comm?.WhatsAppProvider, "WhatsAppBusiness", StringComparison.OrdinalIgnoreCase)
                ? sp.GetRequiredService<Infrastructure.CustomerSuccess.WhatsAppBusinessDeliveryProvider>()
                : sp.GetRequiredService<Infrastructure.CustomerSuccess.LogWhatsAppDeliveryProvider>();
        });
        services.AddScoped<Infrastructure.CustomerSuccess.LogWhatsAppDeliveryProvider>();
        services.AddScoped<Infrastructure.CustomerSuccess.WhatsAppBusinessDeliveryProvider>();
        services.AddScoped<Application.Autonomous.IOutcomeAttributionService, Autonomous.OutcomeAttributionService>();
        services.AddScoped<IEmailAutomationEngine, Infrastructure.CustomerSuccess.EmailAutomationEngine>();
        services.AddScoped<IWhatsAppAutomationEngine, Infrastructure.CustomerSuccess.WhatsAppAutomationEngine>();
        services.AddScoped<ICustomerJourneyEngine, Infrastructure.CustomerSuccess.CustomerJourneyEngine>();
        services.AddScoped<IExpansionRevenueEngine, Infrastructure.CustomerSuccess.ExpansionRevenueEngine>();
        services.AddScoped<ICustomerSuccessIntelligenceService, Infrastructure.CustomerSuccess.CustomerSuccessIntelligenceService>();
        services.AddScoped<IRetentionAutomationEngine, Infrastructure.CustomerSuccess.RetentionAutomationEngine>();
        services.AddScoped<ICustomerKpiService, Infrastructure.CustomerSuccess.CustomerKpiService>();
        services.AddScoped<IExecutiveCustomerDashboardService, Infrastructure.CustomerSuccess.ExecutiveCustomerDashboardService>();

        services.AddScoped<IProductUsageEventRepository, ProductUsageEventRepository>();
        services.AddScoped<ICustomerFeedbackRepository, CustomerFeedbackRepository>();
        services.AddScoped<ICustomerAnalyticsSnapshotRepository, CustomerAnalyticsSnapshotRepository>();
        services.AddScoped<IProductAnalyticsEngine, Infrastructure.Intelligence.ProductAnalyticsEngine>();
        services.AddScoped<INpsEngine, Infrastructure.Intelligence.NpsEngine>();
        services.AddScoped<ICsatEngine, Infrastructure.Intelligence.CsatEngine>();
        services.AddScoped<ICustomerInsightsEngine, Infrastructure.Intelligence.CustomerInsightsEngine>();
        services.AddScoped<IProductUsageIntelligence, Infrastructure.Intelligence.ProductUsageIntelligence>();
        services.AddScoped<IChurnPredictionV2, Infrastructure.Intelligence.ChurnPredictionV2Service>();
        services.AddScoped<IExpansionIntelligence, Infrastructure.Intelligence.ExpansionIntelligenceService>();
        services.AddScoped<ICustomerSegmentationEngine, Infrastructure.Intelligence.CustomerSegmentationEngine>();
        services.AddScoped<IFeedbackEngine, Infrastructure.Intelligence.FeedbackEngine>();
        services.AddScoped<ICustomerDataMartService, Infrastructure.Intelligence.CustomerDataMartService>();
        services.AddScoped<IExecutiveIntelligenceDashboardService, Infrastructure.Intelligence.ExecutiveIntelligenceDashboardService>();
        services.AddScoped<ICustomerInsightsAgentService, Infrastructure.Intelligence.CustomerInsightsAgentService>();
        services.AddScoped<IIntelligenceAutomationEngine, Infrastructure.Intelligence.IntelligenceAutomationEngine>();

        services.AddScoped<IAiDecisionAuditRepository, AiDecisionAuditRepository>();
        services.AddScoped<IAutonomousPlaybookStateRepository, AutonomousPlaybookStateRepository>();
        services.AddScoped<IBusinessKnowledgeRepository, BusinessKnowledgeRepository>();
        services.AddScoped<IMlFeatureSnapshotRepository, MlFeatureSnapshotRepository>();
        services.AddScoped<IAiDecisionAuditService, Infrastructure.Autonomous.AiDecisionAuditService>();
        services.AddScoped<IBusinessKnowledgeEngine, Infrastructure.Autonomous.BusinessKnowledgeEngine>();
        services.AddScoped<IAutonomousRevenueDecisionEngine, Infrastructure.Autonomous.AutonomousRevenueDecisionEngine>();
        services.AddScoped<INextBestActionEngine, Infrastructure.Autonomous.NextBestActionEngine>();
        services.AddScoped<IAutonomousPlaybookEngine, Infrastructure.Autonomous.AutonomousPlaybookEngine>();
        services.AddScoped<IPredictiveRevenueEngine, Infrastructure.Autonomous.PredictiveRevenueEngine>();
        services.AddScoped<IMlFoundationService, Infrastructure.Autonomous.MlFoundationService>();
        services.AddScoped<IAutonomousCommunicationsEngine, Infrastructure.Autonomous.AutonomousCommunicationsEngine>();
        services.AddScoped<IAutonomousCustomerSuccessEngine, Infrastructure.Autonomous.AutonomousCustomerSuccessEngine>();
        services.AddScoped<IAutonomousOrchestrationEngine, Infrastructure.Autonomous.AutonomousOrchestrationEngine>();
        services.AddScoped<IExecutiveAiDashboardService, Infrastructure.Autonomous.ExecutiveAiDashboardService>();
        services.AddScoped<IRevenueAutonomousAgent, Infrastructure.Autonomous.RevenueAutonomousAgent>();
        services.AddScoped<IRenewalAutonomousAgent, Infrastructure.Autonomous.RenewalAutonomousAgent>();
        services.AddScoped<IChurnAutonomousAgent, Infrastructure.Autonomous.ChurnAutonomousAgent>();
        services.AddScoped<IExpansionAutonomousAgent, Infrastructure.Autonomous.ExpansionAutonomousAgent>();
        services.AddScoped<ICustomerAutonomousAgent, Infrastructure.Autonomous.CustomerAutonomousAgent>();
        services.AddScoped<IOperationsAutonomousAgent, Infrastructure.Autonomous.OperationsAutonomousAgent>();

        services.AddScoped<IMlModelVersionRepository, MlModelVersionRepository>();
        services.AddScoped<IMlPipelineRunRepository, MlPipelineRunRepository>();
        services.AddScoped<IMlDriftReportRepository, MlDriftReportRepository>();
        services.AddScoped<IBusinessKnowledgeGraphEdgeRepository, BusinessKnowledgeGraphEdgeRepository>();
        services.AddScoped<INbaOutcomeRecordRepository, NbaOutcomeRecordRepository>();
        services.AddScoped<IModelRegistryService, Infrastructure.EnterpriseAI.ModelRegistryService>();
        services.AddScoped<IMachineLearningPipelineService, Infrastructure.EnterpriseAI.MachineLearningPipelineService>();
        services.AddScoped<IChurnPredictionModel, Infrastructure.EnterpriseAI.ChurnPredictionModelService>();
        services.AddScoped<IExpansionPredictionModel, Infrastructure.EnterpriseAI.ExpansionPredictionModelService>();
        services.AddScoped<IRevenuePredictionModel, Infrastructure.EnterpriseAI.RevenuePredictionModelService>();
        services.AddScoped<INextBestActionMlScorer, Infrastructure.EnterpriseAI.NextBestActionMlService>();
        services.AddScoped<ISelfLearningEngine, Infrastructure.EnterpriseAI.SelfLearningEngine>();
        services.AddScoped<IMlOpsFoundationService, Infrastructure.EnterpriseAI.MlOpsFoundationService>();
        services.AddScoped<IAiEvaluationFrameworkService, Infrastructure.EnterpriseAI.AiEvaluationFrameworkService>();
        services.AddScoped<IBusinessKnowledgeGraphService, Infrastructure.EnterpriseAI.BusinessKnowledgeGraphService>();
        services.AddScoped<IAutonomousOptimizationEngine, Infrastructure.EnterpriseAI.AutonomousOptimizationEngine>();
        services.AddScoped<IExecutiveAiAnalyticsService, Infrastructure.EnterpriseAI.ExecutiveAiAnalyticsService>();
        services.AddScoped<IAiGovernanceService, Infrastructure.EnterpriseAI.AiGovernanceService>();
        services.AddScoped<IEnterpriseAiCycleService, Infrastructure.EnterpriseAI.EnterpriseAiCycleService>();

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
        services.AddScoped<Application.Common.Imports.ICrmImportService, Imports.CrmImportService>();

        services.AddScoped<Application.Integrations.ITenantIntegrationRepository, Integrations.TenantIntegrationRepository>();
        services.AddScoped<Application.Integrations.IIntegrationHubService, Integrations.IntegrationHubService>();
        services.AddScoped<Application.Integrations.IIntegrationConnector, Integrations.HubSpotConnector>();
        services.AddScoped<Application.Integrations.IIntegrationConnector, Integrations.SalesforceConnector>();
        services.AddScoped<Application.Integrations.IIntegrationConnector, Integrations.GmailConnector>();
        services.AddScoped<Application.Integrations.IIntegrationConnector, Integrations.OutlookConnector>();
        services.AddScoped<Application.Integrations.IIntegrationConnector, Integrations.StripeDataConnector>();

        services.AddScoped<Application.Billing.ITenantBillingRepository, Billing.TenantBillingRepository>();
        services.AddScoped<Application.Billing.IStripeBillingService, Billing.StripeBillingService>();

        services.AddScoped<Application.Trust.IAiApprovalRepository, Trust.AiApprovalRepository>();
        services.AddScoped<Application.Trust.IAiTrustService, Trust.AiTrustService>();
        services.AddScoped<Application.Trust.ITenantTrustPolicyService, Trust.TenantTrustPolicyService>();
        services.AddScoped<Application.Trust.ITrustMetricsService, Trust.TrustMetricsService>();
        services.AddScoped<Application.Autonomous.IAiCommandCenterService, Autonomous.AiCommandCenterService>();
        services.AddScoped<Application.Communications.ICommunicationDeliveryService, Communications.CommunicationDeliveryService>();
        services.AddScoped<Application.Autonomous.IOutcomeFabricService, Autonomous.OutcomeFabricService>();
        services.AddScoped<Application.Integrations.IIntegrationTokenRefreshService, Integrations.IntegrationTokenRefreshService>();
        services.AddScoped<Application.Integrations.ISyncConflictService, Integrations.SyncConflictService>();
        services.AddScoped<Application.DataPlatform.IIdentityResolutionService, DataPlatform.IdentityResolutionService>();
        services.AddScoped<Application.DataPlatform.IIdentityMergeService, DataPlatform.IdentityMergeService>();
        services.AddScoped<Application.DataPlatform.IWarehouseExportService, DataPlatform.WarehouseExportService>();
        services.AddScoped<Application.DataPlatform.ICdpEventStreamService, DataPlatform.CdpEventStreamService>();
        services.AddScoped<Application.Trust.ITrustSlaService, Trust.TrustSlaService>();
        services.AddScoped<Application.Voice.ITwilioVoiceService, Voice.TwilioVoiceService>();
        services.AddScoped<Application.EnterpriseAuth.IScimGroupService, EnterpriseAuth.ScimGroupService>();
        services.AddScoped<Application.EnterpriseAuth.ISamlMetadataService, EnterpriseAuth.SamlMetadataService>();

        services.AddScoped<Application.DataPlatform.ICustomer360Service, DataPlatform.Customer360Service>();
        services.AddScoped<Application.DataPlatform.IDataAcquisitionService, DataPlatform.DataAcquisitionService>();
        services.AddScoped<Application.DataPlatform.IMarketplaceCatalogService, DataPlatform.MarketplaceCatalogService>();

        return services;
    }
}

