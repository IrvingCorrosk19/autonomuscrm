using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

public class DealStrategyAgent
{
    private readonly ISalesIntelligenceService _salesIntelligence;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<DealStrategyAgent> _logger;

    public DealStrategyAgent(
        ISalesIntelligenceService salesIntelligence,
        IAgentConfigurationService agentConfig,
        ILogger<DealStrategyAgent> logger)
    {
        _salesIntelligence = salesIntelligence;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task ProcessDealCreatedEvent(DealCreatedEvent domainEvent, CancellationToken cancellationToken)
        => await RunAsync(domainEvent.TenantId!.Value, domainEvent.DealId, cancellationToken);

    public async Task ProcessDealStageChangedEvent(DealStageChangedEvent domainEvent, CancellationToken cancellationToken)
        => await RunAsync(domainEvent.TenantId!.Value, domainEvent.DealId, cancellationToken);

    private async Task RunAsync(Guid tenantId, Guid dealId, CancellationToken cancellationToken)
    {
        var config = await _agentConfig.GetConfigAsync(tenantId, "DealStrategyAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var actions = await _salesIntelligence.AnalyzeAndActAsync(tenantId, dealId, cancellationToken);
        _logger.LogInformation("DealStrategyAgent: {Count} intelligence actions for deal {DealId}", actions.Count, dealId);
    }
}
