using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.SemanticMemory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Workers;

public sealed class BusinessMemoryConsolidationWorker : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromHours(6);
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BusinessMemoryConsolidationWorker> _logger;

    public BusinessMemoryConsolidationWorker(
        IServiceScopeFactory scopeFactory,
        ILogger<BusinessMemoryConsolidationWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var semantic = scope.ServiceProvider.GetRequiredService<ISemanticMemoryService>();
                var repo = scope.ServiceProvider.GetRequiredService<ISemanticMemoryRepository>();
                var tenantAccessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
                tenantAccessor.BypassTenantFilter = true;

                var tenantIds = await repo.GetTenantIdsForConsolidationAsync(500, stoppingToken);
                foreach (var tenantId in tenantIds)
                {
                    await semantic.IndexBusinessMemorySourcesAsync(tenantId, 30, stoppingToken);
                    var created = await semantic.ConsolidateTenantAsync(tenantId, stoppingToken);
                    if (created > 0)
                        _logger.LogInformation("Consolidated {Count} patterns for tenant {TenantId}", created, tenantId);
                }
            }
            catch (Exception ex) when (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogError(ex, "Business memory consolidation cycle failed");
            }

            await Task.Delay(Interval, stoppingToken);
        }
    }
}
