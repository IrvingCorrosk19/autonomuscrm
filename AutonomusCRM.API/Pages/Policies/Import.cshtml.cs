using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using AutonomusCRM.Application.Policies.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace AutonomusCRM.API.Pages.Policies;

public class ImportModel : PageModel
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<ImportModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ImportModel(IServiceProvider serviceProvider, ILogger<ImportModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("", _localizer["Import_Error_SelectFile"].Value);
                return RedirectToPage("/Policies");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var json = await reader.ReadToEndAsync();
            
            var policies = JsonSerializer.Deserialize<List<PolicyImportDto>>(json);
            
            if (policies == null || !policies.Any())
            {
                ModelState.AddModelError("", _localizer["Import_Error_NoValidPolicies"].Value);
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
            ModelState.AddModelError("", _localizer["Import_Error_ImportPoliciesFailed", ex.Message].Value);
            return RedirectToPage("/Policies");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);

    private class PolicyImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string Expression { get; set; } = string.Empty;
        public string? Description { get; set; }
    }
}
