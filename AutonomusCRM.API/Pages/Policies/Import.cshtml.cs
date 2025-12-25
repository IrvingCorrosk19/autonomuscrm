using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Application.Policies.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace AutonomusCRM.API.Pages.Policies;

public class ImportModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportModel> _logger;

    public ImportModel(IServiceProvider serviceProvider, ILogger<ImportModel> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", "Por favor selecciona un archivo");
                return RedirectToPage("/Policies");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            
            var policies = JsonSerializer.Deserialize<List<PolicyImportDto>>(json);
            
            if (policies == null || !policies.Any())
            {
                ModelState.AddModelError("", "El archivo no contiene políticas válidas");
                return RedirectToPage("/Policies");
            }

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<CreatePolicyCommand, Guid>>();
            var createdCount = 0;

            foreach (var policyDto in policies)
            {
                try
                {
                    var command = new CreatePolicyCommand(tenantId, policyDto.Name, policyDto.Expression, policyDto.Description);
                    await createHandler.HandleAsync(command, CancellationToken.None);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error importing policy {Name}", policyDto.Name);
                }
            }

            return RedirectToPage("/Policies", new { imported = createdCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing policies");
            ModelState.AddModelError("", "Error al importar políticas: " + ex.Message);
            return RedirectToPage("/Policies");
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

    private class PolicyImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}

