using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Domain.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages.Customers;

public class BulkActionsModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BulkActionsModel> _logger;

    public BulkActionsModel(IServiceProvider serviceProvider, ILogger<BulkActionsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string customerIds, string action, string? status = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(customerIds))
            {
                return RedirectToPage("/Customers");
            }

            var customerIdList = customerIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            if (!customerIdList.Any())
            {
                return RedirectToPage("/Customers");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            if (action == "updateStatus" && !string.IsNullOrWhiteSpace(status) && Enum.TryParse<CustomerStatus>(status, out var customerStatus))
            {
                var command = new BulkUpdateCustomerStatusCommand(customerIdList, tenantId, customerStatus);
                var handler = _serviceProvider.GetRequiredService<IRequestHandler<BulkUpdateCustomerStatusCommand, int>>();
                
                var updatedCount = await handler.HandleAsync(command, CancellationToken.None);
                
                return RedirectToPage("/Customers", new { bulkUpdated = updatedCount });
            }
            
            return RedirectToPage("/Customers");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk action");
            return RedirectToPage("/Customers");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

