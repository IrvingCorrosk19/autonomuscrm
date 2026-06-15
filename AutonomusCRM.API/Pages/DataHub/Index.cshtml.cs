using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class IndexModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IServiceProvider _sp;

    public IndexModel(IDataHubRepository repo, IServiceProvider sp)
    {
        _repo = repo;
        _sp = sp;
    }

    public IReadOnlyList<DataHubJobSummaryDto> RecentJobs { get; private set; } = Array.Empty<DataHubJobSummaryDto>();

    public IReadOnlyList<(string Icon, string Title, string Description, string Href)> Modules { get; } =
    [
        ("📥", "Import Center", "Guided 10-step wizard — upload to finish", "/DataHub/Wizard"),
        ("🗺", "Mapping Studio", "Visual column → field mapping", "/DataHub/Mapping"),
        ("⚙", "Rules Engine", "IF/THEN cleaning rules — no code", "/DataHub/Rules"),
        ("✓", "Validation Center", "Errors, warnings and data checks", "/DataHub/Validation"),
        ("◎", "Data Quality Center", "Duplicates, score 0–100, fix issues", "/DataHub/Quality"),
        ("🔀", "Duplicate Resolution", "Merge, skip, update or create — enterprise dedup", "/DataHub/Duplicates"),
        ("⏱", "Jobs Monitor", "Live progress, speed and ETA", "/DataHub/Jobs"),
        ("📋", "Import History", "All completed imports", "/DataHub/History"),
        ("⚠", "Error Center", "Filter, export, retry failed rows", "/DataHub/Errors"),
        ("↩", "Rollback Center", "Full, batch or row rollback", "/DataHub/Rollback"),
        ("📑", "Templates Center", "Save & reuse import setups", "/DataHub/Templates")
    ];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var jobs = await _repo.ListJobsAsync(tenantId, 10, cancellationToken);
        RecentJobs = jobs.Select(DataHubOrchestrator.ToSummary).ToList();
    }
}
