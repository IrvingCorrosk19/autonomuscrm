using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Leads.Commands;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;
using System.Text.Json;
using System.Text;

namespace AutonomusCRM.API.Pages.Leads;

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
                return RedirectToPage("/Leads");
            }

            var guard = Application.Common.Imports.ImportGuard.ValidateFile(file.Length, file.FileName);
            if (!guard.Ok)
            {
                return RedirectToPage("/Leads", new { importError = guard.ErrorKey });
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
                ModelState.AddModelError("", _localizer["Import_Error_UnsupportedFormat"].Value);
                return RedirectToPage("/Leads");
            }
            
            var rowCheck = Application.Common.Imports.ImportGuard.ValidateRowCount(leads.Count);
            if (!rowCheck.Ok)
            {
                return RedirectToPage("/Leads", new { importError = rowCheck.ErrorKey });
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
            ModelState.AddModelError("", _localizer["Import_Error_ImportLeadsFailed", ex.Message].Value);
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
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);

    private class LeadImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
        public string? Source { get; set; }
    }
}
