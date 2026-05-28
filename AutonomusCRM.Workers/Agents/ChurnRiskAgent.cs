using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

public class ChurnRiskAgent
{
    private readonly ICustomerSuccessIntelligenceService _intelligence;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<ChurnRiskAgent> _logger;

    public ChurnRiskAgent(
        ICustomerSuccessIntelligenceService intelligence,
        IAgentConfigurationService agentConfig,
        ILogger<ChurnRiskAgent> logger)
    {
        _intelligence = intelligence;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task ProcessRiskUpdatedAsync(CustomerRiskScoreUpdatedEvent evt, CancellationToken cancellationToken)
    {
        if (evt.TenantId == null || evt.RiskScore < 60)
            return;

        var config = await _agentConfig.GetConfigAsync(evt.TenantId.Value, "ChurnRiskAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var actions = await _intelligence.RunChurnIntelligenceAsync(evt.TenantId.Value, evt.CustomerId, cancellationToken);
        _logger.LogInformation("ChurnRiskAgent: {Count} actions for {CustomerId} risk={Risk}", actions.Count, evt.CustomerId, evt.RiskScore);
    }
}
