using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class ImportModel : PageModel
{
    private readonly IDataHubOrchestrator _orchestrator;
    private readonly IServiceProvider _sp;

    public ImportModel(IDataHubOrchestrator orchestrator, IServiceProvider sp)
    {
        _orchestrator = orchestrator;
        _sp = sp;
    }

    public string[] Entities { get; } = ["Customer", "Lead", "Deal", "User"];
    public string SelectedEntity { get; set; } = "Customer";
    public string? Message { get; set; }
    public string? Error { get; set; }
    public Guid? LastJobId { get; set; }

    public void OnGet(Guid? jobId)
    {
        if (jobId.HasValue) LastJobId = jobId;
    }

    public async Task<IActionResult> OnPostAsync(IFormFile file, string targetEntity, string loadMode, bool dryRun, CancellationToken cancellationToken)
    {
        SelectedEntity = targetEntity;
        try
        {
            if (file == null || file.Length == 0)
            {
                Error = "Please select a file.";
                return Page();
            }

            var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
            var userId = Guid.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

            await using var stream = file.OpenReadStream();
            var result = await _orchestrator.UploadAsync(tenantId, userId, stream, file.FileName, targetEntity, loadMode, dryRun, cancellationToken);
            LastJobId = result.JobId;
            Message = $"File parsed successfully. {result.PreviewRowCount}+ rows detected. Status: {result.Status}";
            return RedirectToPage("/DataHub/Wizard", new { jobId = result.JobId, step = 2 });
        }
        catch (Exception ex)
        {
            Error = ex.Message;
            return Page();
        }
    }
}
