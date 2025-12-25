using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Application.Tenants.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Deals;

public class DetailsModel : PageModel
{
    public DealDto? Deal { get; set; }
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<DetailsModel> _logger;

    public DetailsModel(IServiceProvider serviceProvider, ILogger<DetailsModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var query = new GetDealsByTenantQuery(tenantId, null);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>>();
            var deals = await handler.HandleAsync(query, CancellationToken.None);
            
            Deal = deals.FirstOrDefault(d => d.Id == id);
            
            if (Deal == null)
            {
                return NotFound();
            }
            
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading deal details");
            return RedirectToPage("/Deals");
        }
    }

    public async Task<IActionResult> OnPostUpdateProbabilityAsync(Guid id, int probability)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AutonomusCRM.Application.Deals.Commands.UpdateDealProbabilityCommand(id, tenantId, probability);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Deals.Commands.UpdateDealProbabilityCommand, bool>>();
            var result = await handler.HandleAsync(command, CancellationToken.None);
            
            if (result)
            {
                TempData["SuccessMessage"] = "Probabilidad actualizada exitosamente.";
            }
            
            return RedirectToPage("/Deals/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating deal probability");
            TempData["ErrorMessage"] = "Error al actualizar la probabilidad: " + ex.Message;
            return RedirectToPage("/Deals/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostUpdateStageAsync(Guid id, AutonomusCRM.Domain.Deals.DealStage stage, int? probability)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AutonomusCRM.Application.Deals.Commands.UpdateDealStageCommand(id, tenantId, stage, probability);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Deals.Commands.UpdateDealStageCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Deals/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating deal stage");
            return RedirectToPage("/Deals/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostCloseDealAsync(Guid id, decimal? finalAmount)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AutonomusCRM.Application.Deals.Commands.CloseDealCommand(id, tenantId, finalAmount);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Deals.Commands.CloseDealCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Deals/Details", new { id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error closing deal");
            return RedirectToPage("/Deals/Details", new { id });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new AutonomusCRM.Application.Deals.Commands.DeleteDealCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Deals.Commands.DeleteDealCommand, bool>>();
            await handler.HandleAsync(command, CancellationToken.None);
            return RedirectToPage("/Deals");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting deal");
            return RedirectToPage("/Deals/Details", new { id });
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
                var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<CreateTenantCommand, Guid>>();
                var tenantId = await createHandler.HandleAsync(
                    new CreateTenantCommand("Default Tenant", "default@autonomuscrm.com"),
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

