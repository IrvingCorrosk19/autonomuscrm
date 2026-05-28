using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Workers.Agents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEventBus _eventBus;
    private readonly IServiceScopeFactory _scopeFactory;

    public Worker(
        ILogger<Worker> logger,
        IEventBus eventBus,
        IServiceScopeFactory scopeFactory)
    {
        _logger = logger;
        _eventBus = eventBus;
        _scopeFactory = scopeFactory;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Autonomous Agents Worker starting (scoped processing + tenant isolation)...");

        await _eventBus.SubscribeAsync<LeadCreatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<LeadIntelligenceAgent>().ProcessLeadCreatedEvent(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<LeadScoreUpdatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<IRevenueAutomationEngine>().ProcessEventAsync(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<CustomerCreatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<CustomerRiskAgent>().ProcessCustomerCreatedEvent(evt, ct);
            await scope.ServiceProvider.GetRequiredService<CustomerHealthAgent>().ProcessCustomerEventAsync(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<AutonomusCRM.Domain.Customers.Events.CustomerRiskScoreUpdatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<ChurnRiskAgent>().ProcessRiskUpdatedAsync(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<DealCreatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<DealStrategyAgent>().ProcessDealCreatedEvent(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<DealStageChangedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<DealStrategyAgent>().ProcessDealStageChangedEvent(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<CustomerCreatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<CommunicationAgent>().ProcessCustomerCreatedEvent(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<LeadCreatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<CommunicationAgent>().ProcessLeadCreatedEvent(evt, ct);
        }, stoppingToken);

        await _eventBus.SubscribeAsync<IDomainEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<ComplianceSecurityAgent>().ProcessDomainEvent(evt, ct);
        }, stoppingToken);

        _logger.LogInformation("All agents subscribed. Worker heartbeat active.");

        while (!stoppingToken.IsCancellationRequested)
        {
            _logger.LogDebug("Worker heartbeat {Utc}", DateTime.UtcNow);
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var tenantRepo = scope.ServiceProvider.GetRequiredService<ITenantRepository>();
                var all = await tenantRepo.GetAllAsync(stoppingToken);
                foreach (var tenant in all)
                {
                    using var tenantScope = _scopeFactory.CreateScope();
                    tenantScope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>().TenantId = tenant.Id;
                    await tenantScope.ServiceProvider.GetRequiredService<IRevenueAutomationEngine>()
                        .RunPeriodicRevenueScanAsync(tenant.Id, stoppingToken);
                    await tenantScope.ServiceProvider.GetRequiredService<IDataQualityRevenueService>()
                        .ScanAndCreateTasksAsync(tenant.Id, stoppingToken);
                    await tenantScope.ServiceProvider.GetRequiredService<IRetentionAutomationEngine>()
                        .RunPeriodicRetentionScanAsync(tenant.Id, stoppingToken);
                    await tenantScope.ServiceProvider.GetRequiredService<RenewalAgent>()
                        .RunTenantRenewalScanAsync(tenant.Id, stoppingToken);
                    await tenantScope.ServiceProvider.GetRequiredService<ExpansionAgent>()
                        .RunTenantExpansionScanAsync(tenant.Id, stoppingToken);
                    await tenantScope.ServiceProvider.GetRequiredService<IIntelligenceAutomationEngine>()
                        .RunPeriodicIntelligenceScanAsync(tenant.Id, stoppingToken);
                    await tenantScope.ServiceProvider.GetRequiredService<CustomerInsightsAgent>()
                        .RunTenantScanAsync(tenant.Id, stoppingToken);
                    await tenantScope.ServiceProvider.GetRequiredService<IAutonomousOrchestrationEngine>()
                        .RunAutonomousCycleAsync(tenant.Id, stoppingToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Periodic scan failed");
            }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }

        _logger.LogInformation("Autonomous Agents Worker stopping...");
    }

    private static void SetTenant(IServiceScope scope, IDomainEvent evt)
    {
        var tenantAccessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        tenantAccessor.TenantId = evt.TenantId;
        tenantAccessor.CorrelationId = evt.CorrelationId?.ToString();
    }
}
