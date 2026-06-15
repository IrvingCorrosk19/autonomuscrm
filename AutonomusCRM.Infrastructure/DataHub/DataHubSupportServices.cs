using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubSecurityService : IDataHubSecurityService
{
    public (bool Ok, string? Error) ValidateUpload(string fileName, long fileSize, string? contentType)
    {
        if (fileSize <= 0) return (false, "Empty file");
        if (fileSize > DataHubConstants.MaxFileBytes)
            return (false, $"File exceeds maximum size of {DataHubConstants.MaxFileBytes / 1024 / 1024} MB");

        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (!DataHubConstants.AllowedExtensions.Contains(ext))
            return (false, $"Extension '{ext}' is not allowed");

        if (fileName.Contains("..") || fileName.Contains('/') || fileName.Contains('\\'))
            return (false, "Invalid file name");

        if (!string.IsNullOrWhiteSpace(contentType) &&
            !DataHubConstants.AllowedMimeTypes.Any(m => contentType.StartsWith(m, StringComparison.OrdinalIgnoreCase)))
        {
            // Allow unknown MIME if extension is valid
        }

        return (true, null);
    }

    public string SanitizeCellValue(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        var trimmed = value.TrimStart();
        if (trimmed.Length > 0 && "=+-@\t\r".Contains(trimmed[0]))
            return "'" + value;
        return value;
    }
}

public sealed class DataHubFieldCatalogImpl : IDataHubFieldCatalog
{
    public static readonly DataHubFieldCatalogImpl Instance = new();

    public IReadOnlyList<DataHubFieldDefinition> GetFields(string targetEntity)
        => targetEntity switch
        {
            "Customer" => DataHubFieldCatalogDefaults.CustomerFields,
            "Lead" => DataHubFieldCatalogDefaults.LeadFields,
            "Deal" => DataHubFieldCatalogDefaults.DealFields,
            "User" => DataHubFieldCatalogDefaults.UserFields,
            "WorkflowTask" => DataHubFieldCatalogDefaults.WorkflowTaskFields,
            "Policy" => DataHubFieldCatalogDefaults.PolicyFields,
            "Workflow" => DataHubFieldCatalogDefaults.WorkflowFields,
            _ => DataHubFieldCatalogDefaults.CustomerFields
        };

    public DataHubAutoMapResult SuggestMappings(string targetEntity, IReadOnlyList<string> sourceColumns)
    {
        var matches = DataHubSmartMatchingEngine.MatchColumns(
            targetEntity, sourceColumns, Array.Empty<Dictionary<string, string?>>());
        var mappings = new List<DataHubMappingDto>();
        var matched = 0;
        var usedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var match in matches.OrderByDescending(m => m.ConfidencePercent))
        {
            if (match.TargetField == null || match.ConfidencePercent < 60) continue;
            if (!usedTargets.Add(match.TargetField)) continue;
            mappings.Add(new DataHubMappingDto(null, match.SourceColumn, match.TargetField, false, null, null));
            matched++;
        }

        return new DataHubAutoMapResult(mappings, matched, sourceColumns.Count - matched);
    }
}

public static class DataHubFieldCatalog
{
    public static IDataHubFieldCatalog Instance => DataHubFieldCatalogImpl.Instance;
}

public sealed class DataHubExportService : IDataHubExportService
{
    private readonly ApplicationDbContext _db;
    private readonly IDataHubRepository _repo;

    private static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> EntityColumns = new Dictionary<string, IReadOnlyList<string>>(StringComparer.OrdinalIgnoreCase)
    {
        ["Customer"] = ["Name", "Email", "Phone", "Company", "Status"],
        ["Lead"] = ["Name", "Email", "Phone", "Company", "Source"],
        ["Deal"] = ["Title", "Amount", "Stage"],
        ["User"] = ["Email", "FirstName", "LastName"]
    };

    public DataHubExportService(ApplicationDbContext db, IDataHubRepository repo)
    {
        _db = db;
        _repo = repo;
    }

    public async Task ExportToStreamAsync(Guid tenantId, string entityType, string format, Stream output, Dictionary<string, string>? filters, CancellationToken cancellationToken = default)
    {
        var columns = EntityColumns.GetValueOrDefault(entityType) ?? EntityColumns["Customer"];
        var formatNorm = format.ToLowerInvariant();

        switch (formatNorm)
        {
            case "json":
                await ExportJsonStreamAsync(columns, StreamEntityRowsAsync(tenantId, entityType, cancellationToken), output, cancellationToken);
                break;
            case "xlsx":
                await ExportXlsxStreamAsync(columns, StreamEntityRowsAsync(tenantId, entityType, cancellationToken), output, cancellationToken);
                break;
            default:
                await ExportCsvStreamAsync(columns, StreamEntityRowsAsync(tenantId, entityType, cancellationToken), output, cancellationToken);
                break;
        }
    }

    public async Task ExportErrorsToStreamAsync(Guid tenantId, Guid jobId, string format, Stream output, CancellationToken cancellationToken = default)
    {
        var columns = new[] { "RowNumber", "ErrorCode", "FieldName", "Message", "RawValue" };
        var formatNorm = format.ToLowerInvariant();

        async IAsyncEnumerable<Dictionary<string, string?>> StreamErrors()
        {
            var skip = 0;
            while (true)
            {
                var batch = await _repo.GetErrorsAsync(tenantId, jobId, skip, DataHubConstants.ExportStreamBatchSize, cancellationToken);
                if (batch.Count == 0) yield break;
                foreach (var e in batch)
                {
                    yield return new Dictionary<string, string?>
                    {
                        ["RowNumber"] = e.RowNumber.ToString(),
                        ["ErrorCode"] = e.ErrorCode,
                        ["FieldName"] = e.FieldName,
                        ["Message"] = e.Message,
                        ["RawValue"] = e.RawValue
                    };
                }
                skip += batch.Count;
                if (batch.Count < DataHubConstants.ExportStreamBatchSize) yield break;
            }
        }

        switch (formatNorm)
        {
            case "json":
                await ExportJsonStreamAsync(columns, StreamErrors(), output, cancellationToken);
                break;
            case "xlsx":
                await ExportXlsxStreamAsync(columns, StreamErrors(), output, cancellationToken);
                break;
            default:
                await ExportCsvStreamAsync(columns, StreamErrors(), output, cancellationToken);
                break;
        }
    }

    public async IAsyncEnumerable<Dictionary<string, string?>> StreamEntityRowsAsync(
        Guid tenantId, string entityType, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        Guid? lastId = null;
        while (true)
        {
            var batch = entityType switch
            {
                "Customer" => await FetchCustomerBatchAsync(tenantId, lastId, cancellationToken),
                "Lead" => await FetchLeadBatchAsync(tenantId, lastId, cancellationToken),
                "Deal" => await FetchDealBatchAsync(tenantId, lastId, cancellationToken),
                "User" => await FetchUserBatchAsync(tenantId, lastId, cancellationToken),
                _ => new List<(Guid Id, Dictionary<string, string?> Row)>()
            };

            if (batch.Count == 0) yield break;
            foreach (var (_, row) in batch)
                yield return row;
            lastId = batch[^1].Id;
            if (batch.Count < DataHubConstants.ExportStreamBatchSize) yield break;
        }
    }

    private async Task<List<(Guid Id, Dictionary<string, string?> Row)>> FetchCustomerBatchAsync(
        Guid tenantId, Guid? lastId, CancellationToken cancellationToken)
    {
        var query = _db.Customers.Where(c => c.TenantId == tenantId);
        if (lastId.HasValue) query = query.Where(c => c.Id > lastId.Value);
        return (await query.OrderBy(c => c.Id).Take(DataHubConstants.ExportStreamBatchSize)
            .Select(c => new { c.Id, c.Name, c.Email, c.Phone, c.Company, Status = c.Status.ToString() })
            .ToListAsync(cancellationToken))
            .Select(c => (c.Id, new Dictionary<string, string?>
            {
                ["Name"] = c.Name, ["Email"] = c.Email, ["Phone"] = c.Phone,
                ["Company"] = c.Company, ["Status"] = c.Status
            }))
            .ToList();
    }

    private async Task<List<(Guid Id, Dictionary<string, string?> Row)>> FetchLeadBatchAsync(
        Guid tenantId, Guid? lastId, CancellationToken cancellationToken)
    {
        var query = _db.Leads.Where(l => l.TenantId == tenantId);
        if (lastId.HasValue) query = query.Where(l => l.Id > lastId.Value);
        return (await query.OrderBy(l => l.Id).Take(DataHubConstants.ExportStreamBatchSize)
            .Select(l => new { l.Id, l.Name, l.Email, l.Phone, l.Company, Source = l.Source.ToString() })
            .ToListAsync(cancellationToken))
            .Select(l => (l.Id, new Dictionary<string, string?>
            {
                ["Name"] = l.Name, ["Email"] = l.Email, ["Phone"] = l.Phone,
                ["Company"] = l.Company, ["Source"] = l.Source
            }))
            .ToList();
    }

    private async Task<List<(Guid Id, Dictionary<string, string?> Row)>> FetchDealBatchAsync(
        Guid tenantId, Guid? lastId, CancellationToken cancellationToken)
    {
        var query = _db.Deals.Where(d => d.TenantId == tenantId);
        if (lastId.HasValue) query = query.Where(d => d.Id > lastId.Value);
        return (await query.OrderBy(d => d.Id).Take(DataHubConstants.ExportStreamBatchSize)
            .Select(d => new { d.Id, d.Title, d.Amount, Stage = d.Stage.ToString() })
            .ToListAsync(cancellationToken))
            .Select(d => (d.Id, new Dictionary<string, string?>
            {
                ["Title"] = d.Title,
                ["Amount"] = d.Amount.ToString(CultureInfo.InvariantCulture),
                ["Stage"] = d.Stage
            }))
            .ToList();
    }

    private async Task<List<(Guid Id, Dictionary<string, string?> Row)>> FetchUserBatchAsync(
        Guid tenantId, Guid? lastId, CancellationToken cancellationToken)
    {
        var query = _db.Users.Where(u => u.TenantId == tenantId);
        if (lastId.HasValue) query = query.Where(u => u.Id > lastId.Value);
        return (await query.OrderBy(u => u.Id).Take(DataHubConstants.ExportStreamBatchSize)
            .Select(u => new { u.Id, u.Email, u.FirstName, u.LastName })
            .ToListAsync(cancellationToken))
            .Select(u => (u.Id, new Dictionary<string, string?>
            {
                ["Email"] = u.Email, ["FirstName"] = u.FirstName, ["LastName"] = u.LastName
            }))
            .ToList();
    }

    private static async Task ExportCsvStreamAsync(
        IReadOnlyList<string> columns, IAsyncEnumerable<Dictionary<string, string?>> rows, Stream output, CancellationToken cancellationToken)
    {
        await DataHubExportStreaming.WriteCsvHeaderAsync(output, columns, cancellationToken);
        await foreach (var row in rows.WithCancellation(cancellationToken))
            await DataHubExportStreaming.WriteCsvRowAsync(output, columns, row, cancellationToken);
    }

    private static async Task ExportJsonStreamAsync(
        IReadOnlyList<string> columns, IAsyncEnumerable<Dictionary<string, string?>> rows, Stream output, CancellationToken cancellationToken)
    {
        await DataHubExportStreaming.WriteJsonArrayStartAsync(output, cancellationToken);
        var isFirst = true;
        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            await DataHubExportStreaming.WriteJsonRowAsync(output, row, isFirst, cancellationToken);
            isFirst = false;
        }
        await DataHubExportStreaming.WriteJsonArrayEndAsync(output, cancellationToken);
    }

    private static Task ExportXlsxStreamAsync(
        IReadOnlyList<string> columns, IAsyncEnumerable<Dictionary<string, string?>> rows, Stream output, CancellationToken cancellationToken)
        => DataHubExportStreaming.WriteXlsxStreamAsync(output, columns, rows, cancellationToken);
}
