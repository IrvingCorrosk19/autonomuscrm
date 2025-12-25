using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Leads;

public class BulkActionsModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BulkActionsModel> _logger;

    public BulkActionsModel(IServiceProvider serviceProvider, ILogger<BulkActionsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string leadIds, string action, string? status = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(leadIds))
            {
                return RedirectToPage("/Leads");
            }

            var leadIdList = leadIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            if (!leadIdList.Any())
            {
                return RedirectToPage("/Leads");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            if (action == "updateStatus" && !string.IsNullOrWhiteSpace(status) && Enum.TryParse<LeadStatus>(status, out var leadStatus))
            {
                var command = new BulkUpdateLeadStatusCommand(leadIdList, tenantId, leadStatus);
                var handler = _serviceProvider.GetRequiredService<IRequestHandler<BulkUpdateLeadStatusCommand, int>>();
                
                var updatedCount = await handler.HandleAsync(command, CancellationToken.None);
                
                return RedirectToPage("/Leads", new { bulkUpdated = updatedCount });
            }
            
            return RedirectToPage("/Leads");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk action");
            return RedirectToPage("/Leads");
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

