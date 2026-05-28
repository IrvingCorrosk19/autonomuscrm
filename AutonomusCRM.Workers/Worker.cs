using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
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

        await _eventBus.SubscribeAsync<CustomerCreatedEvent>(async (evt, ct) =>
        {
            using var scope = _scopeFactory.CreateScope();
            SetTenant(scope, evt);
            await scope.ServiceProvider.GetRequiredService<CustomerRiskAgent>().ProcessCustomerCreatedEvent(evt, ct);
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
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
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
