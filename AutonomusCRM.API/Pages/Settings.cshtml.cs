using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using AutonomusCRM.Application.System.Commands;
using AutonomusCRM.Application.System.Queries;
using AutonomusCRM.Application.Tenants.Commands;
using AutonomusCRM.Domain.Tenants;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.DependencyInjection;
using AutonomusCRM.API.Infrastructure;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;
using System.Text.Json;

namespace AutonomusCRM.API.Pages;

[Authorize(Roles = "Admin,Manager")]
public class SettingsModel : PageModel
{
    public Tenant? CurrentTenant { get; set; }
    public Guid TenantId { get; set; }
    public Dictionary<string, object> SystemSettings { get; set; } = new();
    
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SettingsModel> _logger;
    private readonly IStringLocalizer<SharedResource> _localizer;

    public SettingsModel(IServiceProvider serviceProvider, ILogger<SettingsModel> logger, IStringLocalizer<SharedResource> localizer)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        _localizer = localizer;
    }

    public async Task OnGetAsync()
    {
        try
        {
            TenantId = await GetDefaultTenantIdAsync();
            
            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync(CancellationToken.None);
            CurrentTenant = tenants.FirstOrDefault(t => t.Id == TenantId);

            var settingsQuery = new GetSystemSettingsQuery(TenantId);
            var settingsHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetSystemSettingsQuery, Dictionary<string, object>>>();
            SystemSettings = await settingsHandler.HandleAsync(settingsQuery, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading settings");
        }
    }

    public async Task<IActionResult> OnPostUpdateTenantAsync(string? name, string? email, string? region, string? timeZone)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var command = new UpdateTenantCommand(tenantId, name, email, region, timeZone);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateTenantCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            TempData["SuccessMessage"] = _localizer["Flash_SettingsSaved"].Value;
            return RedirectToPage("/Settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating tenant");
            TempData["ErrorMessage"] = _localizer["Flash_SettingsUpdateError", ex.Message].Value;
            return RedirectToPage("/Settings");
        }
    }

    public async Task<IActionResult> OnPostUpdateSettingsAsync(string settingsJson)
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(settingsJson) ?? new();
            
            var command = new UpdateSystemSettingsCommand(tenantId, settings);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateSystemSettingsCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            TempData["SuccessMessage"] = _localizer["Flash_SystemSettingsSaved"].Value;
            return RedirectToPage("/Settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            TempData["ErrorMessage"] = _localizer["Flash_SettingsUpdateError", ex.Message].Value;
            return RedirectToPage("/Settings");
        }
    }

    public async Task<IActionResult> OnPostExportConfigAsync()
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var settingsQuery = new GetSystemSettingsQuery(tenantId);
            var settingsHandler = _serviceProvider.GetRequiredService<IRequestHandler<GetSystemSettingsQuery, Dictionary<string, object>>>();
            var settings = await settingsHandler.HandleAsync(settingsQuery, CancellationToken.None);

            var tenantRepository = _serviceProvider.GetRequiredService<ITenantRepository>();
            var tenants = await tenantRepository.GetAllAsync(CancellationToken.None);
            var tenant = tenants.FirstOrDefault(t => t.Id == tenantId);

            var config = new
            {
                Tenant = tenant != null ? new { tenant.Id, tenant.Name, Email = tenant.Settings.GetValueOrDefault("Email", "") } : null,
                Settings = settings,
                ExportedAt = DateTime.UtcNow
            };

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions { WriteIndented = true });
            return File(System.Text.Encoding.UTF8.GetBytes(json), "application/json", $"config-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error exporting config");
            TempData["ErrorMessage"] = _localizer["Flash_ExportConfigError", ex.Message].Value;
            return RedirectToPage("/Settings");
        }
    }

    public async Task<IActionResult> OnPostRestoreDefaultsAsync()
    {
        try
        {
            var tenantId = await GetDefaultTenantIdAsync();
            var defaultSettings = new Dictionary<string, object>
            {
                ["Region"] = "us-east-1",
                ["TimeZone"] = "America/Panama",
                ["MfaRequired"] = true,
                ["KillSwitch"] = false,
                ["MinConfidence"] = 0.75,
                ["OperationMode"] = "Supervised"
            };

            var command = new UpdateSystemSettingsCommand(tenantId, defaultSettings);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateSystemSettingsCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            TempData["SuccessMessage"] = _localizer["Flash_RestoreDefaultsSuccess"].Value;
            return RedirectToPage("/Settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error restoring defaults");
            TempData["ErrorMessage"] = _localizer["Flash_RestoreDefaultsError", ex.Message].Value;
            return RedirectToPage("/Settings");
        }
    }

    public async Task<IActionResult> OnPostImportConfigAsync(IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = _localizer["Flash_SelectFile"].Value;
                return RedirectToPage("/Settings");
            }

            using var stream = file.OpenReadStream();
            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync();
            
            var config = JsonSerializer.Deserialize<Dictionary<string, object>>(content);
            
            if (config == null || !config.ContainsKey("Settings"))
            {
                TempData["ErrorMessage"] = _localizer["Flash_InvalidFile"].Value;
                return RedirectToPage("/Settings");
            }

            var tenantId = await GetDefaultTenantIdAsync();
            var settings = JsonSerializer.Deserialize<Dictionary<string, object>>(config["Settings"].ToString() ?? "{}") ?? new();
            
            var command = new UpdateSystemSettingsCommand(tenantId, settings);
            var handler = _serviceProvider.GetRequiredService<IRequestHandler<UpdateSystemSettingsCommand, bool>>();
            
            await handler.HandleAsync(command, CancellationToken.None);
            
            TempData["SuccessMessage"] = _localizer["Flash_SettingsSaved"].Value;
            return RedirectToPage("/Settings");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error importing config");
            TempData["ErrorMessage"] = _localizer["Flash_ImportError", ex.Message].Value;
            return RedirectToPage("/Settings");
        }
    }
    private Task<Guid> GetDefaultTenantIdAsync(CancellationToken cancellationToken = default)
        => this.GetTenantIdForPageAsync(_serviceProvider, cancellationToken);
}
