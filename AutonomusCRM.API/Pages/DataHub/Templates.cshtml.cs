using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class TemplatesModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IDataHubTemplateVersionService _versions;
    private readonly IServiceProvider _sp;

    public TemplatesModel(
        IDataHubRepository repo,
        IDataHubOrchestrator orchestrator,
        IDataHubTemplateVersionService versions,
        IServiceProvider sp)
    {
        _repo = repo;
        _orchestrator = orchestrator;
        _versions = versions;
        _sp = sp;
    }

    public IReadOnlyList<DataHubTemplateSummaryDto> Templates { get; set; } = Array.Empty<DataHubTemplateSummaryDto>();
    public IReadOnlyList<DataHubTemplateVersionDto> SelectedVersions { get; set; } = Array.Empty<DataHubTemplateVersionDto>();
    public DataHubTemplateVersionCompareDto? CompareResult { get; set; }
    public Guid? JobIdToSave { get; set; }
    public Guid? SelectedTemplateId { get; set; }

    public async Task OnGetAsync(Guid? jobId, Guid? templateId, int? compareA, int? compareB, CancellationToken cancellationToken)
    {
        JobIdToSave = jobId;
        SelectedTemplateId = templateId;
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var raw = await _repo.GetTemplatesAsync(tenantId, cancellationToken);
        Templates = raw.Select(t => new DataHubTemplateSummaryDto(
            t.Id, t.Name, t.TargetEntity, t.Mappings.Count, t.UpdatedAt, t.ActiveVersion, t.LatestVersion)).ToList();

        if (templateId.HasValue)
        {
            SelectedVersions = await _versions.ListVersionsAsync(tenantId, templateId.Value, cancellationToken);
            if (compareA.HasValue && compareB.HasValue)
                CompareResult = await _versions.CompareVersionsAsync(tenantId, templateId.Value, compareA.Value, compareB.Value, cancellationToken);
        }
    }

    public async Task<IActionResult> OnPostAsync(Guid jobId, string templateName, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _orchestrator.SaveTemplateFromJobAsync(tenantId, jobId, templateName, cancellationToken);
        return RedirectToPage(new { saved = true });
    }

    public async Task<IActionResult> OnPostCreateVersionAsync(Guid templateId, string? summary, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        await _versions.CreateVersionAsync(tenantId, userId, templateId, summary, cancellationToken);
        return RedirectToPage(new { templateId });
    }

    public async Task<IActionResult> OnPostRestoreAsync(Guid templateId, int versionNumber, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        await _versions.RestoreVersionAsync(tenantId, userId, templateId, versionNumber, cancellationToken);
        return RedirectToPage(new { templateId });
    }

    public async Task<IActionResult> OnPostActivateAsync(Guid templateId, int versionNumber, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id) ? id : Guid.Empty;
        await _versions.ActivateVersionAsync(tenantId, userId, templateId, versionNumber, cancellationToken);
        return RedirectToPage(new { templateId });
    }
}
