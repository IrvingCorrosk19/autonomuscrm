using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Commands;
using AutonomusCRM.Domain.Deals;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Deals;

public class BulkActionsModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BulkActionsModel> _logger;

    public BulkActionsModel(IServiceProvider serviceProvider, ILogger<BulkActionsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string dealIds, string action, string? stage = null, int? probability = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(dealIds))
            {
                return RedirectToPage("/Deals");
            }

            var dealIdList = dealIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            if (!dealIdList.Any())
            {
                return RedirectToPage("/Deals");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            if (action == "updateStage" && !string.IsNullOrWhiteSpace(stage) && Enum.TryParse<DealStage>(stage, out var dealStage))
            {
                var command = new BulkUpdateDealStageCommand(dealIdList, tenantId, dealStage, probability);
                var handler = _serviceProvider.GetRequiredService<IRequestHandler<BulkUpdateDealStageCommand, int>>();
                
                var updatedCount = await handler.HandleAsync(command, CancellationToken.None);
                
                return RedirectToPage("/Deals", new { bulkUpdated = updatedCount });
            }
            
            return RedirectToPage("/Deals");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk action");
            return RedirectToPage("/Deals");
        }
    }

    private async Task<Guid> GetDefaultTenantIdAsync()
    {
        try
        {
            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync(CancellationToken.None);
            var tenant = tenants.FirstOrDefault();
            
            if (tenant == null)
            {
                var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand, Guid>>();
                var tenantId = await createHandler.HandleAsync(
                    new AutonomusCRM.Application.Tenants.Commands.CreateTenantCommand("Default Tenant", "default@autonomuscrm.com"),
                    CancellationToken.None);
                return tenantId;
            }
            
            return tenant.Id;
        }
        catch
        {
            return Guid.Empty;
        }
    }
}

