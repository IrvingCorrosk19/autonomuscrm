using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.Autonomous;

public interface IAutonomousPlatformGate
{
    Task<bool> IsAutonomousExecutionAllowedAsync(Guid tenantId, CancellationToken cancellationToken = default);
}

public sealed class AutonomousPlatformGate : IAutonomousPlatformGate
{
    private readonly IOptions<AutonomousPlatformOptions> _options;
    private readonly ITenantRepository _tenants;
    private readonly ILogger<AutonomousPlatformGate> _logger;

    public AutonomousPlatformGate(
        IOptions<AutonomousPlatformOptions> options,
        ITenantRepository tenants,
        ILogger<AutonomousPlatformGate> logger)
    {
        _options = options;
        _tenants = tenants;
        _logger = logger;
    }

    public async Task<bool> IsAutonomousExecutionAllowedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        if (!_options.Value.Enabled)
        {
            _logger.LogDebug("Autonomous execution disabled globally for tenant {TenantId}", tenantId);
            return false;
        }

        if (await _tenants.IsKillSwitchEnabledAsync(tenantId, cancellationToken))
        {
            _logger.LogWarning("Autonomous execution blocked by tenant kill-switch {TenantId}", tenantId);
            return false;
        }

        return true;
    }
}
