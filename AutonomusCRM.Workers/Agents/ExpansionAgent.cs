using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

public class ExpansionAgent
{
    private readonly IExpansionRevenueEngine _expansionEngine;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<ExpansionAgent> _logger;

    public ExpansionAgent(
        IExpansionRevenueEngine expansionEngine,
        IAgentConfigurationService agentConfig,
        ILogger<ExpansionAgent> logger)
    {
        _expansionEngine = expansionEngine;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task RunTenantExpansionScanAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var config = await _agentConfig.GetConfigAsync(tenantId, "ExpansionAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var created = await _expansionEngine.CreateExpansionTasksAsync(tenantId, cancellationToken);
        _logger.LogInformation("ExpansionAgent: {Created} expansion tasks for tenant {TenantId}", created, tenantId);
    }
}
