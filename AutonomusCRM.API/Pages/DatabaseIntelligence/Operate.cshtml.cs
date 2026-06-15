using System.Text.Json;
using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class OperateModel : PageModel
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    private readonly IDbConnectionProfileService _connections;
    private readonly IDbOperationService _operations;
    private readonly IServiceProvider _sp;

    public OperateModel(
        IDbConnectionProfileService connections,
        IDbOperationService operations,
        IServiceProvider sp)
    {
        _connections = connections;
        _operations = operations;
        _sp = sp;
    }

    [BindProperty(SupportsGet = true)]
    public Guid ConnectionId { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? JobId { get; set; }

    [BindProperty]
    public OperateFormInput Plan { get; set; } = new();

    public Guid TenantId { get; private set; }
    public Guid SelectedConnectionId { get; private set; }
    public IReadOnlyList<DbConnectionProfileDto> Connections { get; private set; } = Array.Empty<DbConnectionProfileDto>();
    public DbOperationJobDto? Job { get; private set; }
    public DbOperationResultDto? Result { get; private set; }
    public DbOperationPreviewResultDto? Preview { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool CanExecuteImport => User.IsInRole("Admin") || User.IsInRole("Owner");
    public bool HasActiveSession => JobId != null && JobId != Guid.Empty;

    public async Task OnGetAsync(CancellationToken cancellationToken) => await LoadAsync(cancellationToken);

    public async Task<IActionResult> OnGetStartSessionAsync(CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (ConnectionId == Guid.Empty) { await LoadAsync(cancellationToken); return Page(); }

        try
        {
            Job = await _operations.StartSessionAsync(
                TenantId, GetUserId(), ConnectionId,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            JobId = Job.Id;
            StatusMessage = $"Session ready — {Job.TotalRows} rows loaded. Choose your actions below.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostPreviewAsync(CancellationToken cancellationToken) =>
        await RunWithPlanAsync(
            async (tenantId, jobId, plan) =>
            {
                Preview = await _operations.PreviewAsync(tenantId, jobId, plan, cancellationToken);
                StatusMessage = $"Preview — {Preview.AffectedRows} rows affected, {Preview.ExcludedRows} excluded, {Preview.MergedRows} merged.";
            },
            cancellationToken);

    public async Task<IActionResult> OnPostExecuteAsync(CancellationToken cancellationToken)
    {
        if (!CanExecuteImport)
        {
            StatusMessage = "Only administrators can execute and import.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        return await RunWithPlanAsync(
            async (tenantId, jobId, plan) =>
            {
                Result = await _operations.ExecuteAsync(
                    tenantId, GetUserId(), jobId, plan,
                    HttpContext.Connection.RemoteIpAddress?.ToString(),
                    Request.Headers.UserAgent.ToString(),
                    cancellationToken);
                StatusMessage = $"Completed — {Result.ImportedRows} imported, {Result.MergedRows} merged, {Result.ExcludedRows} excluded.";
            },
            cancellationToken);
    }

    public async Task<IActionResult> OnPostRollbackAsync(CancellationToken cancellationToken)
    {
        if (!CanExecuteImport)
        {
            StatusMessage = "Only administrators can rollback imports.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        if (JobId == null || JobId == Guid.Empty)
        {
            StatusMessage = "Start a session before rollback.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        try
        {
            var rollback = await _operations.RollbackAsync(
                TenantId, GetUserId(), JobId.Value,
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);
            StatusMessage = $"Rollback complete — {rollback.DeletedEntities} removed, {rollback.RestoredEntities} restored.";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task<IActionResult> RunWithPlanAsync(
        Func<Guid, Guid, DbOperationActionPlan, Task> action,
        CancellationToken cancellationToken)
    {
        if (JobId == null || JobId == Guid.Empty)
        {
            StatusMessage = "Start a session before running operations.";
            await LoadAsync(cancellationToken);
            return Page();
        }

        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        try
        {
            var plan = OperatePlanBuilder.Build(Plan);
            await action(TenantId, JobId.Value, plan);
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }

        await LoadAsync(cancellationToken);
        return Page();
    }

    private Guid GetUserId() =>
        Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var uid) ? uid : Guid.Empty;

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        TenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Connections = await _connections.ListAsync(TenantId, cancellationToken);
        SelectedConnectionId = ConnectionId != Guid.Empty ? ConnectionId : Connections.FirstOrDefault()?.Id ?? Guid.Empty;

        if (JobId == null || JobId == Guid.Empty) return;

        Job = await _operations.GetJobAsync(TenantId, JobId.Value, cancellationToken);
        Result = await _operations.GetResultAsync(TenantId, JobId.Value, cancellationToken);

        if (!string.IsNullOrWhiteSpace(Job?.PlanJson))
        {
            try
            {
                var saved = JsonSerializer.Deserialize<DbOperationActionPlan>(Job.PlanJson, JsonOptions);
                if (saved != null)
                    OperatePlanBuilder.ApplyToForm(Plan, saved);
            }
            catch (JsonException) { /* keep posted values */ }
        }
    }
}
