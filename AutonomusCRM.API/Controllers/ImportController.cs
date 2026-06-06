using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using ImportGuard = AutonomusCRM.Application.Common.Imports.ImportGuard;
using AutonomusCRM.API.Resources;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/import")]
[Authorize]
public class ImportController : ControllerBase
{
    private readonly IStringLocalizer<SharedResource> _localizer;

    public ImportController(IStringLocalizer<SharedResource> localizer)
    {
        _localizer = localizer;
    }

    [HttpPost("customers")]
    public async Task<ActionResult<ImportResultDto>> ImportCustomers(
        [FromQuery] Guid tenantId,
        IFormFile? file,
        [FromBody] List<CustomerImportRow>? body,
        CancellationToken cancellationToken)
    {
        var rows = await ResolveRowsAsync(file, body, ParseCustomerCsv, cancellationToken);
        if (rows == null) return BadRequest(ImportGuardMessage());
        var guard = Validate(rows.Count, file);
        if (guard != null) return BadRequest(guard);

        var svc = HttpContext.RequestServices.GetRequiredService<ICrmImportService>();
        return Ok(await svc.ImportCustomersAsync(tenantId, rows, cancellationToken));
    }

    [HttpPost("leads")]
    public async Task<ActionResult<ImportResultDto>> ImportLeads(
        [FromQuery] Guid tenantId,
        IFormFile? file,
        [FromBody] List<LeadImportRow>? body,
        CancellationToken cancellationToken)
    {
        var rows = await ResolveRowsAsync(file, body, ParseLeadCsv, cancellationToken);
        if (rows == null) return BadRequest(ImportGuardMessage());
        var guard = Validate(rows.Count, file);
        if (guard != null) return BadRequest(guard);

        var svc = HttpContext.RequestServices.GetRequiredService<ICrmImportService>();
        return Ok(await svc.ImportLeadsAsync(tenantId, rows, cancellationToken));
    }

    [HttpPost("deals")]
    public async Task<ActionResult<ImportResultDto>> ImportDeals(
        [FromQuery] Guid tenantId,
        IFormFile? file,
        [FromBody] List<DealImportRow>? body,
        CancellationToken cancellationToken)
    {
        var rows = await ResolveRowsAsync(file, body, ParseDealCsv, cancellationToken);
        if (rows == null) return BadRequest(ImportGuardMessage());
        var guard = Validate(rows.Count, file);
        if (guard != null) return BadRequest(guard);

        var svc = HttpContext.RequestServices.GetRequiredService<ICrmImportService>();
        return Ok(await svc.ImportDealsAsync(tenantId, rows, cancellationToken));
    }

    private string? Validate(int rowCount, IFormFile? file)
    {
        if (file != null)
        {
            var g = ImportGuard.ValidateFile(file.Length, file.FileName);
            if (!g.Ok) return LocalizeGuardError(g);
        }
        var rc = ImportGuard.ValidateRowCount(rowCount);
        return rc.Ok ? null : LocalizeGuardError(rc);
    }

    private string ImportGuardMessage() => _localizer["Import_Api_GuardMessage"].Value;

    private string LocalizeGuardError((bool Ok, string? ErrorKey, object[]? FormatArgs) guard)
    {
        if (guard.FormatArgs is { Length: > 0 })
            return _localizer[guard.ErrorKey!, guard.FormatArgs].Value;
        return _localizer[guard.ErrorKey!].Value;
    }

    private static async Task<List<T>?> ResolveRowsAsync<T>(
        IFormFile? file,
        List<T>? body,
        Func<string, List<T>> parseCsv,
        CancellationToken cancellationToken)
    {
        if (body is { Count: > 0 })
            return body;

        if (file == null || file.Length == 0)
            return null;

        await using var stream = file.OpenReadStream();
        using var reader = new StreamReader(stream, Encoding.UTF8);
        var content = await reader.ReadToEndAsync(cancellationToken);

        if (file.FileName.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            return JsonSerializer.Deserialize<List<T>>(content, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return parseCsv(content);

        return null;
    }

    private static List<CustomerImportRow> ParseCustomerCsv(string content)
    {
        var rows = new List<CustomerImportRow>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var start = lines[0].Contains("Name", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        for (var i = start; i < lines.Length; i++)
        {
            var f = lines[i].Split(',');
            if (f.Length >= 1 && !string.IsNullOrWhiteSpace(f[0]))
                rows.Add(new CustomerImportRow(f[0].Trim(), f.Length > 1 ? f[1].Trim() : null, f.Length > 2 ? f[2].Trim() : null, f.Length > 3 ? f[3].Trim() : null));
        }
        return rows;
    }

    private static List<LeadImportRow> ParseLeadCsv(string content)
    {
        var rows = new List<LeadImportRow>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var start = lines[0].Contains("Name", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        for (var i = start; i < lines.Length; i++)
        {
            var f = lines[i].Split(',');
            if (f.Length >= 1 && !string.IsNullOrWhiteSpace(f[0]))
                rows.Add(new LeadImportRow(f[0].Trim(), f.Length > 1 ? f[1].Trim() : "Other", f.Length > 2 ? f[2].Trim() : null, f.Length > 3 ? f[3].Trim() : null, f.Length > 4 ? f[4].Trim() : null));
        }
        return rows;
    }

    private static List<DealImportRow> ParseDealCsv(string content)
    {
        var rows = new List<DealImportRow>();
        var lines = content.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var start = lines[0].Contains("Title", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        for (var i = start; i < lines.Length; i++)
        {
            var f = lines[i].Split(',');
            if (f.Length >= 2 && !string.IsNullOrWhiteSpace(f[0]) && decimal.TryParse(f[1], out var amount))
                rows.Add(new DealImportRow(f[0].Trim(), amount, f.Length > 2 ? f[2].Trim() : "Prospecting", f.Length > 3 ? f[3].Trim() : null));
        }
        return rows;
    }
}
