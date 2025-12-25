using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Application.Policies.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;

namespace AutonomusCRM.API.Pages.Policies;

public class EditModel : PageModel
{
    public Policy? Policy { get; set; }
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EditModel> _logger;

    public EditModel(IServiceProvider serviceProvider, ILogger<EditModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnGetAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var policyRepository = _serviceProvider.GetRequiredService<IPolicyRepository>();
            var policy = await policyRepository.GetByIdAsync(id, CancellationToken.None);
            
            if (policy == null || policy.TenantId != tenantId)
            {
                return NotFound();
            }

            Policy = policy;
            return Page();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading policy for edit");
            return RedirectToPage("/Policies");
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid id, string name, string expression, string? description, bool? isActive)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new UpdatePolicyCommand(id, tenantId, name, expression, description, isActive);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdatePolicyCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Policies");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating policy");
            ModelState.AddModelError("", "Error al actualizar la política: " + ex.Message);
            
            // Recargar datos
            var tenantId = await GetDefaultTenantIdAsync();
            var policyRepository = _serviceProvider.GetRequiredService<IPolicyRepository>();
            var policy = await policyRepository.GetByIdAsync(id, CancellationToken.None);
            if (policy != null && policy.TenantId == tenantId)
            {
                Policy = policy;
            }
            
            return Page();
        }
    }

    public async Task<IActionResult> OnPostDuplicateAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new DuplicatePolicyCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<DuplicatePolicyCommand, Guid>>();
            
            var newPolicyId = await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Policies/Edit", new { id = newPolicyId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error duplicating policy");
            return RedirectToPage("/Policies/Edit", new { id });
        }
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid id)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new DeletePolicyCommand(id, tenantId);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<DeletePolicyCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            return RedirectToPage("/Policies");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting policy");
            return RedirectToPage("/Policies/Edit", new { id });
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

