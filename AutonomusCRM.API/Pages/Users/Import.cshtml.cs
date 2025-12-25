using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Users.Commands;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using System.Text;

namespace AutonomusCRM.API.Pages.Users;

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
                return RedirectToPage("/Users");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            
            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            List<UserImportDto> users;
            
            // Detectar si es JSON o CSV
            if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                users = JsonSerializer.Deserialize<List<UserImportDto>>(content) ?? new();
            }
            else if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            {
                users = ParseCsv(content);
            }
            else
            {
                ModelState.AddModelError("", "Formato de archivo no soportado. Use JSON o CSV");
                return RedirectToPage("/Users");
            }
            
            if (!users.Any())
            {
                ModelState.AddModelError("", "El archivo no contiene usuarios válidos");
                return RedirectToPage("/Users");
            }

            var createHandler = _serviceProvider.GetRequiredService<IRequestHandler<CreateUserCommand, Guid>>();
            var createdCount = 0;

            foreach (var userDto in users)
            {
                try
                {
                    // Hash simple de contraseña (en producción usar BCrypt o similar)
                    var passwordHash = Convert.ToBase64String(Encoding.UTF8.GetBytes(userDto.Password ?? "DefaultPassword123!"));
                    
                    var command = new CreateUserCommand(tenantId, userDto.Email, passwordHash, userDto.FirstName, userDto.LastName);
                    await createHandler.HandleAsync(command, CancellationToken.None);
                    createdCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error importing user {Email}", userDto.Email);
                }
            }

            return RedirectToPage("/Users", new { imported = createdCount });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing users");
            ModelState.AddModelError("", "Error al importar usuarios: " + ex.Message);
            return RedirectToPage("/Users");
        }
    }

    private List<UserImportDto> ParseCsv(string content)
    {
        var users = new List<UserImportDto>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        
        // Saltar header si existe
        var startIndex = lines[0].Contains("Email", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        
        for (int i = startIndex; i < lines.Length; i++)
        {
            var fields = lines[i].Split(',');
            if (fields.Length >= 1 && !string.IsNullOrWhiteSpace(fields[0]))
            {
                users.Add(new UserImportDto
                {
                    Email = fields[0].Trim(),
                    FirstName = fields.Length > 1 ? fields[1].Trim() : null,
                    LastName = fields.Length > 2 ? fields[2].Trim() : null,
                    Password = fields.Length > 3 ? fields[3].Trim() : "DefaultPassword123!"
                });
            }
        }
        
        return users;
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

    private class UserImportDto
    {
        public string Email { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Password { get; set; }
    }
}

