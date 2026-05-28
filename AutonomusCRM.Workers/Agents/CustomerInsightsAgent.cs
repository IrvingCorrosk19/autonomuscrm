using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Intelligence;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

public class CustomerInsightsAgent
{
    private readonly ICustomerInsightsAgentService _agentService;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<CustomerInsightsAgent> _logger;

    public CustomerInsightsAgent(
        ICustomerInsightsAgentService agentService,
        IAgentConfigurationService agentConfig,
        ILogger<CustomerInsightsAgent> logger)
    {
        _agentService = agentService;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task RunTenantScanAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var config = await _agentConfig.GetConfigAsync(tenantId, "CustomerInsightsAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var actions = await _agentService.AnalyzeAndActAsync(tenantId, cancellationToken);
        _logger.LogInformation("CustomerInsightsAgent: {Count} actions for tenant {TenantId}", actions.Count, tenantId);
    }
}
