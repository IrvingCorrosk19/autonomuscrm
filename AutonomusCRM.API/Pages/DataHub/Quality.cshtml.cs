using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class QualityModel : PageModel
{
    private readonly IDataHubQualityScoreService _scoreService;
    private readonly IDataHubQualityActionService _actions;
    private readonly ApplicationDbContext _db;
    private readonly IServiceProvider _sp;

    public QualityModel(
        IDataHubQualityScoreService scoreService,
        IDataHubQualityActionService actions,
        ApplicationDbContext db,
        IServiceProvider sp)
    {
        _scoreService = scoreService;
        _actions = actions;
        _db = db;
        _sp = sp;
    }

    public DataHubQualityScoreDto Score { get; private set; } = new(100, "Excellent", 0, 0, 0, Array.Empty<DataHubQualityIssueDto>());
    public IReadOnlyList<(Guid Id, string Email)> TeamMembers { get; private set; } = Array.Empty<(Guid, string)>();
    public string? Message { get; set; }
    public string? Error { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostMergeAsync(Guid keepId, string? mergeIds, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var ids = ParseIds(mergeIds);
        var result = await _actions.MergeCustomersAsync(tenantId, keepId, ids, cancellationToken);
        SetFeedback(result);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAssignAsync(Guid leadId, Guid userId, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var result = await _actions.AssignLeadOwnerAsync(tenantId, leadId, userId, cancellationToken);
        SetFeedback(result);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAutoAssignAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var result = await _actions.AutoAssignLeadsAsync(tenantId, cancellationToken);
        SetFeedback(result);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostMarkReviewAsync(string entityType, Guid entityId, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var result = await _actions.MarkForReviewAsync(tenantId, entityType, entityId, cancellationToken);
        SetFeedback(result);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostKeepBothAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var result = await _actions.KeepDuplicatesAsync(tenantId, entityId, cancellationToken);
        SetFeedback(result);
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostExportIssueAsync(string issueCode, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Score = await _scoreService.CalculateScoreAsync(tenantId, cancellationToken);
        var issues = Score.TopIssues.Where(i => i.IssueCode == issueCode).ToList();
        if (issues.Count == 0)
        {
            await LoadAsync(cancellationToken);
            Error = "No issues to export.";
            return Page();
        }

        var csv = "EntityType,EntityId,IssueCode,Message\n" +
            string.Join("\n", issues.Select(i => $"{i.EntityType},{i.EntityId},{i.IssueCode},\"{i.Message.Replace("\"", "'")}\""));
        return File(System.Text.Encoding.UTF8.GetBytes(csv), "text/csv", $"quality-{issueCode.ToLowerInvariant()}.csv");
    }

    private void SetFeedback(DataHubQualityActionResultDto result)
    {
        if (result.Success) Message = result.Message;
        else Error = result.Message;
    }

    private static IReadOnlyList<Guid> ParseIds(string? raw)
        => string.IsNullOrWhiteSpace(raw)
            ? Array.Empty<Guid>()
            : raw.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(s => Guid.TryParse(s, out var g) ? g : Guid.Empty)
                .Where(g => g != Guid.Empty)
                .ToList();

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Score = await _scoreService.CalculateScoreAsync(tenantId, cancellationToken);
        TeamMembers = await _db.Users
            .Where(u => u.TenantId == tenantId && u.IsActive)
            .OrderBy(u => u.Email)
            .Select(u => new ValueTuple<Guid, string>(u.Id, u.Email))
            .ToListAsync(cancellationToken);
    }
}
