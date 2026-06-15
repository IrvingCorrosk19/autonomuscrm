using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class MappingModel : PageModel
{
    private readonly IDataHubRepository _repo;
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IDataHubFieldCatalog _fields;
    private readonly IServiceProvider _sp;

    public MappingModel(IDataHubRepository repo, IDataHubOrchestrator orchestrator, IDataHubFieldCatalog fields, IServiceProvider sp)
    {
        _repo = repo;
        _orchestrator = orchestrator;
        _fields = fields;
        _sp = sp;
    }

    public Guid? JobId { get; set; }
    public string TargetEntity { get; set; } = "Customer";
    public List<DataHubMappingDto> Mappings { get; set; } = new();
    public IReadOnlyList<DataHubFieldDefinition> TargetFields { get; private set; } = Array.Empty<DataHubFieldDefinition>();

    public async Task OnGetAsync(Guid? jobId, CancellationToken cancellationToken)
    {
        JobId = jobId;
        if (!jobId.HasValue) return;
        await LoadAsync(jobId.Value, cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(Guid jobId, List<DataHubMappingDto> mappings, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _orchestrator.SaveMappingsAsync(tenantId, jobId, mappings, cancellationToken);
        return RedirectToPage(new { jobId });
    }

    public async Task<IActionResult> OnPostAutoMapAsync(Guid jobId, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _orchestrator.AutoMapAsync(tenantId, jobId, cancellationToken);
        return RedirectToPage(new { jobId });
    }

    private async Task LoadAsync(Guid jobId, CancellationToken ct)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, ct);
        var job = await _repo.GetJobAsync(tenantId, jobId, ct);
        if (job == null) return;
        TargetEntity = job.TargetEntity;
        TargetFields = _fields.GetFields(TargetEntity);
        var mappings = await _repo.GetMappingsAsync(tenantId, jobId, ct);
        Mappings = mappings.Select(m => new DataHubMappingDto(m.Id, m.SourceColumn, m.TargetField, m.IsRequired, m.DefaultValue, m.TransformRule)).ToList();
        if (Mappings.Count == 0)
        {
            var auto = _fields.SuggestMappings(TargetEntity, job.DetectedColumns);
            Mappings = auto.Mappings.ToList();
        }
    }
}
