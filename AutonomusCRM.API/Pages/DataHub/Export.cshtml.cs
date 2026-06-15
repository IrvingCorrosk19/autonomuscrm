using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class ExportModel : PageModel
{
    private readonly IDataHubExportService _export;
    private readonly IDataHubSecurityQuotaService _quotas;
    private readonly IDataHubForensicAuditService _forensic;
    private readonly IServiceProvider _sp;

    public ExportModel(
        IDataHubExportService export,
        IDataHubSecurityQuotaService quotas,
        IDataHubForensicAuditService forensic,
        IServiceProvider sp)
    {
        _export = export;
        _quotas = quotas;
        _forensic = forensic;
        _sp = sp;
    }

    public string[] Entities { get; } = ["Customer", "Lead", "Deal", "User"];

    public async Task<IActionResult> OnGetDownloadAsync(string entityType, string format, CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        await _quotas.EnsureExportAllowedAsync(tenantId, cancellationToken);

        var userId = Guid.TryParse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var id)
            ? id : (Guid?)null;
        await _forensic.RecordAsync(new DataHubForensicAuditEntry(
            tenantId, DataHubForensicActions.Export, userId, null, $"{entityType}.{format}", null, null, null, null,
            new Dictionary<string, object> { ["entityType"] = entityType, ["format"] = format }), cancellationToken);

        var contentType = format switch
        {
            "json" => "application/json",
            "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            _ => "text/csv"
        };
        Response.ContentType = contentType;
        Response.Headers.ContentDisposition = $"attachment; filename=\"{entityType.ToLowerInvariant()}.{format}\"";
        await _export.ExportToStreamAsync(tenantId, entityType, format, Response.Body, null, cancellationToken);
        return new EmptyResult();
    }
}
