using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubScheduledImportWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DataHubScheduledImportWorker> _logger;

    public DataHubScheduledImportWorker(IServiceScopeFactory scopeFactory, ILogger<DataHubScheduledImportWorker> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Data Hub scheduled import worker started");
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
                accessor.BypassTenantFilter = true;
                var service = scope.ServiceProvider.GetRequiredService<IDataHubScheduledImportService>();
                await service.ProcessDueSchedulesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { break; }
            catch (Exception ex) { _logger.LogError(ex, "Scheduled import worker tick failed"); }

            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }
    }
}
