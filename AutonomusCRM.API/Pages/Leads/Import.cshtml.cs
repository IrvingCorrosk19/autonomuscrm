using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;

namespace AutonomusCRM.API.Pages.Leads;

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
                return RedirectToPage("/Leads");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            List<LeadImportDto> leads;
            
            if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                leads = JsonSerializer.Deserialize<List<LeadImportDto>>(content) ?? new();
            }
            else if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                leads = ParseCsv(content);
            }
            else
            {
                ModelState.AddModelError("", "Formato de archivo no soportado. Use JSON o CSV");
                return RedirectToPage("/Leads");
            }
            
            if (!leads.Any())
            {
                ModelState.AddModelError("", "El archivo no contiene leads válidos");
                return RedirectToPage("/Leads");
            }

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<CreateLeadCommand, Guid>>();
            var createdCount = 0;

            foreach (var leadDto in leads)
            {
                try
                {
                    if (!Enum.TryParse<LeadSource>(leadDto.Source ?? "Other", out var source))
                    {
                        source = LeadSource.Other;
                    }
                    
                    var command = new CreateLeadCommand(tenantId, leadDto.Name, source, leadDto.Email, leadDto.Phone, leadDto.Company);
                    await createHandler.HandleAsync(command, CancellationToken.None);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error importing lead {Name}", leadDto.Name);
                }
            }

            return RedirectToPage("/Leads", new { imported = createdCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing leads");
            ModelState.AddModelError("", "Error al importar leads: " + ex.Message);
            return RedirectToPage("/Leads");
        }
    }

    private List<LeadImportDto> ParseCsv(string content)
    {
        var leads = new List<LeadImportDto>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var startIndex = lines[0].Contains("Name", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        
        for (int i = startIndex; i < lines.Length; i++)
        {
            var fields = lines[i].Split(',');
            if (fields.Length >= 1 && !string.IsNullOrWhiteSpace(fields[0]))
            {
                leads.Add(new LeadImportDto
                {
                    Name = fields[0].Trim(),
                    Email = fields.Length > 1 ? fields[1].Trim() : null,
                    Phone = fields.Length > 2 ? fields[2].Trim() : null,
                    Company = fields.Length > 3 ? fields[3].Trim() : null,
                    Source = fields.Length > 4 ? fields[4].Trim() : "Other"
                });
            }
        }
        
        return leads;
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

    private class LeadImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Source { get; set; }
    }
}

