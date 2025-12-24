using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads.Events;
using AutonomusCRM.Domain.Customers.Events;
using AutonomusCRM.Domain.Deals.Events;
using AutonomusCRM.Domain.Events;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Workers.Agents;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IEventBus _eventBus;
    private readonly LeadIntelligenceAgent _leadAgent;
    private readonly CustomerRiskAgent _customerRiskAgent;
    private readonly DealStrategyAgent _dealStrategyAgent;
    private readonly CommunicationAgent _communicationAgent;
    private readonly DataQualityGuardian _dataQualityGuardian;
    private readonly ComplianceSecurityAgent _complianceAgent;
    private readonly AutomationOptimizerAgent _optimizerAgent;

    public Worker(
        ILogger<Worker> logger,
        IEventBus eventBus,
        LeadIntelligenceAgent leadAgent,
        CustomerRiskAgent customerRiskAgent,
        DealStrategyAgent dealStrategyAgent,
        CommunicationAgent communicationAgent,
        DataQualityGuardian dataQualityGuardian,
        ComplianceSecurityAgent complianceAgent,
        AutomationOptimizerAgent optimizerAgent)
    {
        _logger = logger;
        _eventBus = eventBus;
        _leadAgent = leadAgent;
        _customerRiskAgent = customerRiskAgent;
        _dealStrategyAgent = dealStrategyAgent;
        _communicationAgent = communicationAgent;
        _dataQualityGuardian = dataQualityGuardian;
        _complianceAgent = complianceAgent;
        _optimizerAgent = optimizerAgent;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Autonomous Agents Worker starting...");

        // Suscribir agentes a eventos
        await _eventBus.SubscribeAsync<LeadCreatedEvent>(
            async (evt, ct) => await _leadAgent.ProcessLeadCreatedEvent(evt, ct),
            stoppingToken);

        await _eventBus.SubscribeAsync<CustomerCreatedEvent>(
            async (evt, ct) => await _customerRiskAgent.ProcessCustomerCreatedEvent(evt, ct),
            stoppingToken);

        await _eventBus.SubscribeAsync<DealCreatedEvent>(
            async (evt, ct) => await _dealStrategyAgent.ProcessDealCreatedEvent(evt, ct),
            stoppingToken);

        await _eventBus.SubscribeAsync<DealStageChangedEvent>(
            async (evt, ct) => await _dealStrategyAgent.ProcessDealStageChangedEvent(evt, ct),
            stoppingToken);

        await _eventBus.SubscribeAsync<CustomerCreatedEvent>(
            async (evt, ct) => await _communicationAgent.ProcessCustomerCreatedEvent(evt, ct),
            stoppingToken);

        await _eventBus.SubscribeAsync<LeadCreatedEvent>(
            async (evt, ct) => await _communicationAgent.ProcessLeadCreatedEvent(evt, ct),
            stoppingToken);

        // Compliance agent se suscribe a todos los eventos
        await _eventBus.SubscribeAsync<IDomainEvent>(
            async (evt, ct) => await _complianceAgent.ProcessDomainEvent(evt, ct),
            stoppingToken);

        _logger.LogInformation("All agents subscribed to events. Waiting for events...");

        // Tareas periódicas
        _ = Task.Run(async () =>
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
                // Ejecutar análisis periódicos
                // TODO: Obtener lista de tenants activos
                // await _dataQualityGuardian.ScanDataQuality(tenantId, stoppingToken);
                // await _optimizerAgent.AnalyzePerformance(stoppingToken);
            }
        }, stoppingToken);

        // Mantener el worker activo
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }

        _logger.LogInformation("Autonomous Agents Worker stopping...");
    }
}
