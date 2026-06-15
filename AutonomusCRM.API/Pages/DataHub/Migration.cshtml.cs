using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class MigrationModel : PageModel
{
    private readonly IDataHubMigrationService _migration;
    private readonly IServiceProvider _sp;

    public MigrationModel(IDataHubMigrationService migration, IServiceProvider sp)
    {
        _migration = migration;
        _sp = sp;
    }

    public int Step { get; private set; } = 1;
    public string? SelectedSource { get; private set; }
    public IReadOnlyList<DataHubMigrationSourceDto> Sources { get; private set; } = [];
    public IReadOnlyList<DataHubMigrationEntityDto> Entities { get; private set; } = [];
    public DataHubMigrationConnectionStatusDto? ConnectionStatus { get; private set; }
    public List<DataHubMigrationStartResultDto> StartedJobs { get; private set; } = [];
    public string? ErrorMessage { get; private set; }

    public async Task OnGetAsync(int step = 1, string? source = null, CancellationToken cancellationToken = default)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Step = Math.Clamp(step, 1, 4);
        SelectedSource = source;
        Sources = await _migration.ListSourcesAsync(tenantId, cancellationToken);

        if (Step >= 2 && !string.IsNullOrEmpty(SelectedSource))
        {
            ConnectionStatus = await _migration.GetConnectionStatusAsync(tenantId, SelectedSource, cancellationToken);
            Entities = _migration.ListEntities(SelectedSource);
        }
    }

    public async Task<IActionResult> OnPostStartAsync(
        string source, string[] entities, string mode = "Full", string loadMode = "Upsert",
        CancellationToken cancellationToken = default)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        SelectedSource = source;
        Step = 4;
        Sources = await _migration.ListSourcesAsync(tenantId, cancellationToken);
        Entities = _migration.ListEntities(source);

        if (entities == null || entities.Length == 0)
        {
            ErrorMessage = "Select at least one entity to migrate.";
            Step = 3;
            ConnectionStatus = await _migration.GetConnectionStatusAsync(tenantId, source, cancellationToken);
            return Page();
        }

        if (!Enum.TryParse<DataHubMigrationImportMode>(mode, true, out var importMode))
            importMode = DataHubMigrationImportMode.Full;

        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid)
            ? uid : Guid.Empty;

        foreach (var entity in entities)
        {
            try
            {
                var result = await _migration.StartMigrationAsync(new DataHubMigrationRequestDto(
                    tenantId, userId, source, entity, importMode, loadMode, false), cancellationToken);
                StartedJobs.Add(result);
            }
            catch (Exception ex)
            {
                ErrorMessage = (ErrorMessage ?? "") + $"{entity}: {ex.Message}; ";
            }
        }

        return Page();
    }
}
