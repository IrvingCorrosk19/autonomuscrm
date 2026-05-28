using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers.Agents;

public class RenewalAgent
{
    private readonly IRenewalEngine _renewalEngine;
    private readonly IAgentConfigurationService _agentConfig;
    private readonly ILogger<RenewalAgent> _logger;

    public RenewalAgent(
        IRenewalEngine renewalEngine,
        IAgentConfigurationService agentConfig,
        ILogger<RenewalAgent> logger)
    {
        _renewalEngine = renewalEngine;
        _agentConfig = agentConfig;
        _logger = logger;
    }

    public async Task RunTenantRenewalScanAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var config = await _agentConfig.GetConfigAsync(tenantId, "RenewalAgent", cancellationToken);
        if (!_agentConfig.IsEnabled(config))
            return;

        var created = await _renewalEngine.EnforceRenewalWindowsAsync(tenantId, cancellationToken);
        _logger.LogInformation("RenewalAgent: {Created} renewal tasks for tenant {TenantId}", created, tenantId);
    }
}
