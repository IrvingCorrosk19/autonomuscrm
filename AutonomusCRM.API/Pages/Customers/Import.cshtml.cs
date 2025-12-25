using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Customers.Commands;
using AutonomusCRM.Domain.Customers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;

namespace AutonomusCRM.API.Pages.Customers;

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
                return RedirectToPage("/Customers");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            List<CustomerImportDto> customers;
            
            if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                customers = JsonSerializer.Deserialize<List<CustomerImportDto>>(content) ?? new();
            }
            else if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                customers = ParseCsv(content);
            }
            else
            {
                ModelState.AddModelError("", "Formato de archivo no soportado. Use JSON o CSV");
                return RedirectToPage("/Customers");
            }
            
            if (!customers.Any())
            {
                ModelState.AddModelError("", "El archivo no contiene clientes válidos");
                return RedirectToPage("/Customers");
            }

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<CreateCustomerCommand, Guid>>();
            var createdCount = 0;

            foreach (var customerDto in customers)
            {
                try
                {
                    var command = new CreateCustomerCommand(tenantId, customerDto.Name, customerDto.Email, customerDto.Phone, customerDto.Company);
                    await createHandler.HandleAsync(command, CancellationToken.None);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error importing customer {Name}", customerDto.Name);
                }
            }

            return RedirectToPage("/Customers", new { imported = createdCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing customers");
            ModelState.AddModelError("", "Error al importar clientes: " + ex.Message);
            return RedirectToPage("/Customers");
        }
    }

    private List<CustomerImportDto> ParseCsv(string content)
    {
        var customers = new List<CustomerImportDto>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var startIndex = lines[0].Contains("Name", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        
        for (int i = startIndex; i < lines.Length; i++)
        {
            var fields = lines[i].Split(',');
            if (fields.Length >= 1 && !string.IsNullOrWhiteSpace(fields[0]))
            {
                customers.Add(new CustomerImportDto
                {
                    Name = fields[0].Trim(),
                    Email = fields.Length > 1 ? fields[1].Trim() : null,
                    Phone = fields.Length > 2 ? fields[2].Trim() : null,
                    Company = fields.Length > 3 ? fields[3].Trim() : null
                });
            }
        }
        
        return customers;
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

    private class CustomerImportDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Company { get; set; }
    }
}

