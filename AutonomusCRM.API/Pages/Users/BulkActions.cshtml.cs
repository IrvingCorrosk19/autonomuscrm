using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Users.Commands;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;

namespace AutonomusCRM.API.Pages.Users;

[Authorize(Roles = "Admin,Manager")]
public class BulkActionsModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<BulkActionsModel> _logger;

    public BulkActionsModel(IServiceProvider serviceProvider, ILogger<BulkActionsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(string userIds, string action)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(userIds))
            {
                return RedirectToPage("/Users");
            }

            var userIdList = userIds.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(id => Guid.TryParse(id, out var guid) ? guid : Guid.Empty)
                .Where(id => id != Guid.Empty)
                .ToList();

            if (!userIdList.Any())
            {
                return RedirectToPage("/Users");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            var isActive = action == "activate";
            
            var command = new BulkUpdateUserStatusCommand(userIdList, tenantId, isActive);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<BulkUpdateUserStatusCommand, int>>();
            
            var updatedCount = await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Users", new { bulkUpdated = updatedCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing bulk action");
            return RedirectToPage("/Users");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}

