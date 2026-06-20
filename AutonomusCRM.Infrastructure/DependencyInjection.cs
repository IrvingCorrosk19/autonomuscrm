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

        // EF Core 9: solo factory — también registra ApplicationDbContext como scoped.
        // No llamar AddDbContext además; provoca "Cannot resolve scoped IDbContextOptionsConfiguration from root".
        services.AddDbContextFactory<ApplicationDbContext>(options =>
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
        services.AddScoped<ICustomerSuccessOsService, Infrastructure.CustomerSuccess.CustomerSuccessOsService>();
        services.Configure<Application.CustomerSuccess.CommunicationOptions>(
            configuration.GetSection(Application.CustomerSuccess.CommunicationOptions.SectionName));
        services.Configure<Billing.StripeBillingOptions>(configuration.GetSection(Billing.StripeBillingOptions.SectionName));
        services.Configure<Application.EnterpriseAuth.EnterpriseAuthOptions>(
            configuration.GetSection(Application.EnterpriseAuth.EnterpriseAuthOptions.SectionName));
        services.Configure<Application.Integrations.IntegrationOAuthOptions>(
            configuration.GetSection(Application.Integrations.IntegrationOAuthOptions.SectionName));
        services.Configure<Application.Integrations.IntegrationEndpointsOptions>(
            configuration.GetSection(Application.Integrations.IntegrationEndpointsOptions.SectionName));
        services.Configure<Application.Integrations.TwilioOptions>(
            configuration.GetSection(Application.Integrations.TwilioOptions.SectionName));
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
        services.AddScoped<Application.Autonomous.IAbosOutcomeLearningService, Autonomous.AbosOutcomeLearningService>();
        services.AddScoped<Application.Executive.IExecutiveOsService, Executive.ExecutiveOsService>();
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
        services.AddScoped<Application.Revenue.IRevenueOsService, Revenue.RevenueOsService>();
        services.AddScoped<Application.DataPlatform.ICustomer360EnterpriseService, DataPlatform.Customer360EnterpriseService>();
        services.AddScoped<Application.Billing.IBillingDashboardService, Billing.BillingDashboardService>();
        services.AddScoped<Application.Communications.ICommunicationDeliveryService, Communications.CommunicationDeliveryService>();
        services.AddScoped<Application.Autonomous.IOutcomeFabricService, Autonomous.OutcomeFabricService>();
        services.AddSingleton<Application.Integrations.IIntegrationTokenProtector, Integrations.IntegrationTokenProtector>();
        services.AddScoped<Application.Integrations.IIntegrationHealthService, Integrations.IntegrationHealthService>();
        services.AddScoped<Application.Integrations.IIntegrationSmokeTestService, Integrations.IntegrationSmokeTestService>();
        services.AddSingleton<Application.Integrations.ISecretMaskingService, Integrations.SecretMaskingService>();
        services.AddSingleton<Application.Integrations.IIntegrationWebhookAuditor, Integrations.IntegrationWebhookAuditor>();
        services.AddScoped<Application.Integrations.IIntegrationTokenRefreshService, Integrations.IntegrationTokenRefreshService>();
        services.AddScoped<Application.Events.IFailedEventReplayService, Events.FailedEventReplayService>();
        services.AddScoped<Application.Integrations.ISyncConflictService, Integrations.SyncConflictService>();
        services.AddScoped<Application.DataPlatform.IIdentityResolutionService, DataPlatform.IdentityResolutionService>();
        services.AddScoped<Application.DataPlatform.IIdentityMergeService, DataPlatform.IdentityMergeService>();
        services.AddScoped<Application.DataPlatform.IWarehouseExportService, DataPlatform.WarehouseExportService>();
        services.AddScoped<Application.DataPlatform.ICdpEventStreamService, DataPlatform.CdpEventStreamService>();
        services.AddScoped<Application.Trust.ITrustSlaService, Trust.TrustSlaService>();
        services.AddScoped<Application.Voice.ITwilioVoiceService, Voice.TwilioVoiceService>();
        services.AddScoped<Application.EnterpriseAuth.IScimGroupService, EnterpriseAuth.ScimGroupService>();
        services.AddScoped<Application.EnterpriseAuth.ISamlMetadataService, EnterpriseAuth.SamlMetadataService>();
        services.AddScoped<Application.EnterpriseAuth.ISamlAuthService, EnterpriseAuth.SamlAuthService>();

        services.AddScoped<Application.BusinessMemory.IBusinessMemoryRepository, BusinessMemory.BusinessMemoryRepository>();
        services.AddScoped<Application.BusinessMemory.IBusinessMemoryPipeline, BusinessMemory.BusinessMemoryPipeline>();
        services.AddScoped<Application.BusinessMemory.IBusinessMemoryService, BusinessMemory.BusinessMemoryService>();
        services.AddScoped<Application.SemanticMemory.ISemanticMemoryRepository, SemanticMemory.SemanticMemoryRepository>();
        services.AddScoped<Application.SemanticMemory.ISemanticMemoryService, SemanticMemory.SemanticMemoryService>();
        services.AddScoped<Application.KnowledgeGraph.IKnowledgeGraphRepository, KnowledgeGraph.KnowledgeGraphRepository>();
        services.AddScoped<Application.KnowledgeGraph.IKnowledgeGraphService, KnowledgeGraph.KnowledgeGraphService>();
        services.AddScoped<Application.KnowledgeGraph.IOperationalGraphFeed, KnowledgeGraph.OperationalGraphFeedService>();
        services.AddScoped<Application.KnowledgeGraph.IGraphReasoningEngine, KnowledgeGraph.GraphReasoningEngine>();
        services.AddScoped<Application.KnowledgeGraph.IDecisionIntelligenceEngine, Intelligence.DecisionIntelligenceEngine>();
        services.AddScoped<Application.KnowledgeGraph.IBusinessSimulationEngine, KnowledgeGraph.BusinessSimulationEngine>();
        services.AddScoped<Application.KnowledgeGraph.IGraphReasoningFoundation, KnowledgeGraph.GraphReasoningFoundation>();
        services.AddHttpClient();
        services.AddSingleton<Application.SemanticMemory.IProductionEmbeddingProvider, SemanticMemory.ProductionEmbeddingProvider>();
        services.AddSingleton<AutonomusCRM.AI.IEmbeddingService, Ai.ProductionEmbeddingServiceAdapter>();

        services.AddScoped<Application.DataPlatform.ICustomer360Service, DataPlatform.Customer360Service>();
        services.AddScoped<Application.DataPlatform.IDataAcquisitionService, DataPlatform.DataAcquisitionService>();
        services.AddScoped<Application.DataPlatform.IMarketplaceCatalogService, DataPlatform.MarketplaceCatalogService>();

        services.AddScoped<Application.DataHub.IDataHubRepository, DataHub.DataHubRepository>();
        services.AddScoped<Application.DataHub.IDataHubExtractService, DataHub.DataHubExtractService>();
        services.AddScoped<Application.DataHub.IDataHubTransformService, DataHub.DataHubTransformService>();
        services.AddScoped<Application.DataHub.IDataHubValidateService, DataHub.DataHubValidateService>();
        services.AddScoped<Application.DataHub.IDataHubLoadService, DataHub.DataHubLoadService>();
        services.AddScoped<Application.DataHub.IDataHubExportService, DataHub.DataHubExportService>();
        services.AddScoped<Application.DataHub.IDataHubSecurityService, DataHub.DataHubSecurityService>();
        services.AddSingleton<Application.DataHub.IDataHubFieldCatalog, DataHub.DataHubFieldCatalogImpl>();
        services.AddScoped<Application.DataHub.IDataHubMigrationService, DataHub.DataHubMigrationService>();
        services.AddScoped<Application.DataHub.IMigrationSyncCompleter, DataHub.Migration.MigrationSyncCompleter>();
        services.AddScoped<Application.DataHub.IDataHubScheduledImportService, DataHub.DataHubScheduledImportService>();
        services.AddScoped<Application.DataHub.IDataHubTemplateVersionService, DataHub.DataHubTemplateVersionService>();
        services.AddHostedService<DataHub.DataHubScheduledImportWorker>();
        services.AddScoped<DataHub.Migration.MigrationSourceExtractorRegistry>();
        services.AddScoped<Application.DataHub.IMigrationSourceExtractor, DataHub.Migration.SalesforceMigrationExtractor>();
        services.AddScoped<Application.DataHub.IMigrationSourceExtractor, DataHub.Migration.HubSpotMigrationExtractor>();
        services.AddScoped<Application.DataHub.IMigrationSourceExtractor, DataHub.Migration.DynamicsMigrationExtractor>();
        services.AddScoped<Application.DataHub.IMigrationSourceExtractor, DataHub.Migration.ZohoMigrationExtractor>();
        services.AddScoped<Application.DataHub.IMigrationSourceExtractor, DataHub.Migration.PipedriveMigrationExtractor>();
        services.AddScoped<Application.DataHub.IDataHubOrchestrator, DataHub.DataHubOrchestrator>();
        services.AddSingleton<DataHub.DataHubFileStore>();
        services.AddSingleton<Application.DataHub.IDataHubJobQueue, DataHub.DataHubJobQueue>();
        services.AddScoped<Application.DataHub.IDataHubIntelligenceService, DataHub.DataHubIntelligenceService>();
        services.AddScoped<Application.DataHub.IDataHubAutoFixService, DataHub.DataHubAutoFixService>();
        services.AddScoped<Application.DataHub.IDataHubRulesEngineService, DataHub.DataHubRulesEngineService>();
        services.AddScoped<Application.DataHub.IDataHubQualityScoreService, DataHub.DataHubQualityScoreService>();
        services.AddScoped<Application.DataHub.IDataHubQualityActionService, DataHub.DataHubQualityActionService>();
        services.AddScoped<Application.DataHub.IDataHubProgressNotifier, DataHub.NullDataHubProgressNotifier>();
        services.AddScoped<Application.DataHub.IDataHubRollbackService, DataHub.DataHubRollbackService>();
        services.AddScoped<Application.DataHub.IDataHubDuplicateEngine, DataHub.DataHubDuplicateEngine>();

        services.Configure<DataHub.DataHubProcessingOptions>(configuration.GetSection("DataHub"));
        services.Configure<Application.DataHub.DataHubSecurityOptions>(configuration.GetSection(Application.DataHub.DataHubSecurityOptions.SectionName));
        services.AddSingleton<DataHub.DataHubFileEncryption>();
        services.AddScoped<DataHub.HeuristicMalwareScanner>();
        services.AddScoped<Application.DataHub.IDataHubMalwareScanner, DataHub.ClamAvMalwareScanner>();
        services.AddScoped<Application.DataHub.IDataHubTenantGuard, DataHub.DataHubTenantGuard>();
        services.AddScoped<Application.DataHub.IDataHubForensicAuditService, DataHub.DataHubForensicAuditService>();
        services.AddScoped<Application.DataHub.IDataHubSecurityQuotaService, DataHub.DataHubSecurityQuotaService>();
        services.AddScoped<Application.DataHub.IDataHubRequestContext, DataHub.NullDataHubRequestContext>();
        services.AddSingleton<DataHub.DataHubImportDispatcher>();
        services.AddSingleton<Application.DataHub.IDataHubImportDispatcher>(sp => sp.GetRequiredService<DataHub.DataHubImportDispatcher>());

        var dataHubMode = configuration.GetValue<Application.DataHub.DataHubProcessingMode>(
            "DataHub:ProcessingMode", Application.DataHub.DataHubProcessingMode.InProcess);
        if (dataHubMode != Application.DataHub.DataHubProcessingMode.RabbitMQ)
            services.AddHostedService<DataHub.DataHubBackgroundProcessor>();
        services.AddHostedService<DataHub.DataHubOrphanRecoveryWorker>();

        services.Configure<Application.DatabaseIntelligence.DbIntelligenceSecurityOptions>(
            configuration.GetSection(Application.DatabaseIntelligence.DbIntelligenceSecurityOptions.SectionName));
        services.AddSingleton<DatabaseIntelligence.DbConnectorFactory>();
        services.AddSingleton<Application.DatabaseIntelligence.IDbConnectorFactory>(sp => sp.GetRequiredService<DatabaseIntelligence.DbConnectorFactory>());
        services.AddSingleton<Application.DatabaseIntelligence.IDbConnectionVault, DatabaseIntelligence.DbIntelligenceConnectionVault>();
        services.AddScoped<Application.DatabaseIntelligence.IDbIntelligenceTenantGuard, DatabaseIntelligence.DbIntelligenceTenantGuard>();
        services.AddScoped<Application.DatabaseIntelligence.IDbIntelligenceAuditService, DatabaseIntelligence.DbIntelligenceAuditService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbConnectionProfileService, DatabaseIntelligence.DbConnectionProfileService>();

        services.AddSingleton<DatabaseIntelligence.Discovery.DbSchemaIntrospectorRegistry>();
        services.AddScoped<DatabaseIntelligence.Discovery.DbSchemaDiscoveryService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSchemaDiscoveryService>(sp =>
            sp.GetRequiredService<DatabaseIntelligence.Discovery.DbSchemaDiscoveryService>());
        services.AddHostedService<DatabaseIntelligence.Discovery.DbDiscoveryBackgroundWorker>();
        services.AddScoped<Application.DatabaseIntelligence.IBusinessEntityInferenceEngine, DatabaseIntelligence.BusinessDiscovery.BusinessEntityInferenceEngine>();
        services.AddScoped<DatabaseIntelligence.BusinessDiscovery.DbBusinessSampleReader>();
        services.AddScoped<Application.DatabaseIntelligence.IBusinessDiscoveryService, DatabaseIntelligence.BusinessDiscovery.BusinessDiscoveryService>();
        services.AddScoped<Application.DatabaseIntelligence.IDataHealthEngine, DatabaseIntelligence.Health.DataHealthEngine>();
        services.AddScoped<Application.DatabaseIntelligence.IDataHealthService, DatabaseIntelligence.Health.DataHealthService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbBusinessGraphBuilder, DatabaseIntelligence.Graph.DbBusinessGraphBuilder>();
        services.AddScoped<Application.DatabaseIntelligence.IDbBusinessGraphService, DatabaseIntelligence.Graph.DbBusinessGraphService>();
        services.Configure<DatabaseIntelligence.Sync.DbSyncProcessingOptions>(configuration.GetSection("DatabaseIntelligence:Sync"));
        services.AddSingleton<DatabaseIntelligence.Sync.IDbSyncJobQueue, DatabaseIntelligence.Sync.DbSyncInProcessJobQueue>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncOrchestrator, DatabaseIntelligence.Sync.DbSyncOrchestrator>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncPipeline, DatabaseIntelligence.Sync.DbSyncPipeline>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncExtractService, DatabaseIntelligence.Sync.DbSyncExtractService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncStagingService, DatabaseIntelligence.Sync.DbSyncStagingService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncLoadService, DatabaseIntelligence.Sync.DbSyncLoadService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncRollbackService, DatabaseIntelligence.Sync.DbSyncRollbackService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncConflictResolver, DatabaseIntelligence.Sync.DbSyncConflictResolver>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncScheduleService, DatabaseIntelligence.Sync.DbSyncScheduleService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbSyncDispatcher, DatabaseIntelligence.Sync.DbSyncDispatcher>();
        services.AddHostedService<DatabaseIntelligence.Sync.DbSyncBackgroundWorker>();
        services.AddHostedService<DatabaseIntelligence.Sync.DbSyncScheduledWorker>();
        services.AddScoped<Application.DatabaseIntelligence.IDbIntelligenceInsightEngine, DatabaseIntelligence.Insights.DbIntelligenceInsightEngine>();
        services.AddScoped<DatabaseIntelligence.Insights.DbIntelligenceInsightSemanticEnhancer>();
        services.AddScoped<Application.DatabaseIntelligence.IDbIntelligenceInsightService, DatabaseIntelligence.Insights.DbIntelligenceInsightService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbOperationEngine, DatabaseIntelligence.Operations.DbOperationEngine>();
        services.AddScoped<DatabaseIntelligence.Operations.DbOperationRollbackService>();
        services.AddScoped<Application.DatabaseIntelligence.IDbOperationService, DatabaseIntelligence.Operations.DbOperationService>();

        return services;
    }
}

