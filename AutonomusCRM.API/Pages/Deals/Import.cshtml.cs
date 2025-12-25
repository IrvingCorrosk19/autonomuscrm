using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Deals.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;

namespace AutonomusCRM.API.Pages.Deals;

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
                return RedirectToPage("/Deals");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            List<DealImportDto> deals;
            
            if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                deals = JsonSerializer.Deserialize<List<DealImportDto>>(content) ?? new();
            }
            else if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                deals = ParseCsv(content);
            }
            else
            {
                ModelState.AddModelError("", "Formato de archivo no soportado. Use JSON o CSV");
                return RedirectToPage("/Deals");
            }
            
            if (!deals.Any())
            {
                ModelState.AddModelError("", "El archivo no contiene deals válidos");
                return RedirectToPage("/Deals");
            }

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<CreateDealCommand, Guid>>();
            var customerRepository = _serviceProvider.GetRequiredService<ICustomerRepository>();
            var customers = await customerRepository.GetByTenantIdAsync(tenantId);
            var createdCount = 0;

            foreach (var dealDto in deals)
            {
                try
                {
                    Guid customerId;
                    if (Guid.TryParse(dealDto.CustomerId, out var parsedCustomerId))
                    {
                        customerId = parsedCustomerId;
                    }
                    else
                    {
                        // Buscar por email o nombre
                        var customer = customers.FirstOrDefault(c => 
                            c.Email == dealDto.CustomerEmail || c.Name == dealDto.CustomerName);
                        if (customer == null)
                        {
                            // Crear customer si no existe
                            var createCustomerHandler = _serviceProvider.GetRequiredService<IRequestHandler<AutonomusCRM.Application.Customers.Commands.CreateCustomerCommand, Guid>>();
                            customerId = await createCustomerHandler.HandleAsync(
                                new AutonomusCRM.Application.Customers.Commands.CreateCustomerCommand(tenantId, dealDto.CustomerName ?? "Cliente Importado", dealDto.CustomerEmail, null, null),
                                CancellationToken.None);
                        }
                        else
                        {
                            customerId = customer.Id;
                        }
                    }
                    
                    var command = new CreateDealCommand(tenantId, customerId, dealDto.Title, dealDto.Amount, dealDto.Description);
                    await createHandler.HandleAsync(command, CancellationToken.None);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error importing deal {Title}", dealDto.Title);
                }
            }

            return RedirectToPage("/Deals", new { imported = createdCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing deals");
            ModelState.AddModelError("", "Error al importar deals: " + ex.Message);
            return RedirectToPage("/Deals");
        }
    }

    private List<DealImportDto> ParseCsv(string content)
    {
        var deals = new List<DealImportDto>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        var startIndex = lines[0].Contains("Title", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        
        for (int i = startIndex; i < lines.Length; i++)
        {
            var fields = lines[i].Split(',');
            if (fields.Length >= 3 && !string.IsNullOrWhiteSpace(fields[0]))
            {
                deals.Add(new DealImportDto
                {
                    Title = fields[0].Trim(),
                    CustomerId = fields.Length > 1 ? fields[1].Trim() : null,
                    Amount = decimal.TryParse(fields.Length > 2 ? fields[2].Trim() : "0", out var amt) ? amt : 0,
                    Description = fields.Length > 3 ? fields[3].Trim() : null,
                    CustomerName = fields.Length > 4 ? fields[4].Trim() : null,
                    CustomerEmail = fields.Length > 5 ? fields[5].Trim() : null
                });
            }
        }
        
        return deals;
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

    private class DealImportDto
    {
        public string Title { get; set; } = string.Empty;
        public string? CustomerId { get; set; }
        public decimal Amount { get; set; }
        public string? Description { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
    }
}

