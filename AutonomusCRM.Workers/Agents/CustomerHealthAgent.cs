using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Domain.Customers.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

public class CustomerHealthAgent
{
    private readonly ICustomerSuccessIntelligenceService _intelligence;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<CustomerHealthAgent> _logger;

    public CustomerHealthAgent(
        ICustomerSuccessIntelligenceService intelligence,
        IAgentConfigurationService agentConfig,
        ILogger<CustomerHealthAgent> logger)
    {
        _intelligence = intelligence;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task ProcessCustomerEventAsync(CustomerCreatedEvent evt, CancellationToken cancellationToken)
    {
        if (evt.TenantId == null)
            return;

        var config = await _agentConfig.GetConfigAsync(evt.TenantId.Value, "CustomerHealthAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var actions = await _intelligence.RunHealthIntelligenceAsync(evt.TenantId.Value, evt.CustomerId, cancellationToken);
        _logger.LogInformation("CustomerHealthAgent: {Count} actions for {CustomerId}", actions.Count, evt.CustomerId);
    }
}
