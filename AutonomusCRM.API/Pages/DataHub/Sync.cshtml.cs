using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class SyncModel : PageModel
{
    private readonly IDataHubScheduledImportService _schedules;
    private readonly IDataHubMigrationService _migration;
    private readonly IServiceProvider _sp;

    public SyncModel(IDataHubScheduledImportService schedules, IDataHubMigrationService migration, IServiceProvider sp)
    {
        _schedules = schedules;
        _migration = migration;
        _sp = sp;
    }

    public IReadOnlyList<DataHubScheduledImportDto> Schedules { get; set; } = Array.Empty<DataHubScheduledImportDto>();
    public IReadOnlyList<DataHubMigrationSourceDto> Sources { get; set; } = Array.Empty<DataHubMigrationSourceDto>();
    public string[] Frequencies { get; } = Enum.GetNames<DataHubScheduleFrequency>();
    public string[] ImportModes { get; } = Enum.GetNames<DataHubMigrationImportMode>();

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Schedules = await _schedules.ListAsync(tenantId, cancellationToken);
        Sources = await _migration.ListSourcesAsync(tenantId, cancellationToken);
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string name, string source, string sourceEntity, string frequency, string importMode, string loadMode,
        DateTime? runOnceAt, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : Guid.Empty;
        await _schedules.CreateAsync(tenantId, userId, new DataHubScheduledImportCreateDto(
            name, source, sourceEntity, frequency, importMode, loadMode, runOnceAt), cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRunAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : Guid.Empty;
        await _schedules.ExecuteNowAsync(tenantId, userId, scheduleId, cancellationToken);
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostDeleteAsync(Guid scheduleId, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _schedules.DeleteAsync(tenantId, scheduleId, cancellationToken);
        return RedirectToPage();
    }
}
