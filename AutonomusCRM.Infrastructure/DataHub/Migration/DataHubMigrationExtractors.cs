using System.Globalization;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.DataHub.Migration;

public static class MigrationCsvBuilder
{
    public static MemoryStream ToCsvStream(IReadOnlyList<string> columns, IReadOnlyList<Dictionary<string, string?>> rows)
    {
        var ms = new MemoryStream();
        using var writer = new StreamWriter(ms, Encoding.UTF8, leaveOpen: true);
        writer.WriteLine(string.Join(",", columns.Select(CsvEscape)));
        foreach (var row in rows)
            writer.WriteLine(string.Join(",", columns.Select(c => CsvEscape(row.GetValueOrDefault(c) ?? ""))));
        writer.Flush();
        ms.Position = 0;
        return ms;
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}

internal static class MigrationConnectionHelper
{
    public static TenantIntegrationConnectionSnapshot ToSnapshot(TenantIntegrationConnection conn)
        => new(conn.TenantId, conn.Provider, conn.AccessToken, conn.RefreshToken, conn.InstanceUrl,
            conn.Settings, conn.LastSyncAt);

    public static string? GetSetting(TenantIntegrationConnectionSnapshot conn, string key)
        => conn.Settings.TryGetValue(key, out var v) ? v : null;

    public static bool HasToken(TenantIntegrationConnectionSnapshot conn)
        => !string.IsNullOrWhiteSpace(conn.AccessToken) || conn.Settings.ContainsKey("apiToken");
}

public sealed class SalesforceMigrationExtractor : IMigrationSourceExtractor
{
    private readonly IHttpClientFactory _http;
    private readonly ILogger<SalesforceMigrationExtractor> _logger;

    public SalesforceMigrationExtractor(IHttpClientFactory http, ILogger<SalesforceMigrationExtractor> logger)
    {
        _http = http;
        _logger = logger;
    }

    public string Source => "Salesforce";

    public IReadOnlyList<DataHubMigrationEntityDto> SupportedEntities { get; } =
    [
        new("Accounts", "Accounts", "Customer", "Salesforce Account → Customer"),
        new("Contacts", "Contacts", "Customer", "Salesforce Contact → Customer"),
        new("Leads", "Leads", "Lead", "Salesforce Lead → Lead"),
        new("Opportunities", "Opportunities", "Deal", "Salesforce Opportunity → Deal")
    ];

    public bool IsConfigured(TenantIntegrationConnectionSnapshot? connection)
        => connection != null
            && !string.IsNullOrWhiteSpace(connection.AccessToken)
            && !string.IsNullOrWhiteSpace(connection.InstanceUrl);

    public async Task<MigrationExtractResult> ExtractAsync(
        TenantIntegrationConnectionSnapshot connection, string sourceEntity,
        DataHubMigrationImportMode mode, DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        var (soql, columns) = sourceEntity switch
        {
            "Accounts" => BuildSoql("Account", ["Id", "Name", "Phone", "Website", "Industry"], mode, sinceUtc),
            "Contacts" => BuildSoql("Contact", ["Id", "FirstName", "LastName", "Email", "Phone", "AccountId"], mode, sinceUtc),
            "Leads" => BuildSoql("Lead", ["Id", "FirstName", "LastName", "Email", "Phone", "Company", "Status"], mode, sinceUtc),
            "Opportunities" => BuildSoql("Opportunity", ["Id", "Name", "Amount", "StageName", "CloseDate"], mode, sinceUtc),
            _ => throw new ArgumentException($"Unsupported Salesforce entity: {sourceEntity}")
        };

        var rows = new List<Dictionary<string, string?>>();
        var client = _http.CreateClient("Salesforce");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        var url = $"{connection.InstanceUrl!.TrimEnd('/')}/services/data/v59.0/query?q={Uri.EscapeDataString(soql)}";

        while (!string.IsNullOrEmpty(url))
        {
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (doc.RootElement.TryGetProperty("records", out var records))
            {
                foreach (var item in records.EnumerateArray())
                {
                    var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var col in columns)
                    {
                        if (col == "Name" && sourceEntity is "Contacts" or "Leads")
                        {
                            var first = item.TryGetProperty("FirstName", out var fn) ? fn.GetString() : "";
                            var last = item.TryGetProperty("LastName", out var ln) ? ln.GetString() : "";
                            row["Name"] = $"{first} {last}".Trim();
                            continue;
                        }
                        row[col] = item.TryGetProperty(col, out var v) ? FormatJsonValue(v) : null;
                    }
                    if (sourceEntity == "Opportunities" && row.TryGetValue("Name", out var title))
                        row["Title"] = title;
                    rows.Add(row);
                }
            }
            url = doc.RootElement.TryGetProperty("nextRecordsUrl", out var next)
                ? $"{connection.InstanceUrl!.TrimEnd('/')}{next.GetString()}"
                : null;
        }

        _logger.LogInformation("Salesforce migration extracted {Count} {Entity} rows for tenant {TenantId}",
            rows.Count, sourceEntity, connection.TenantId);

        return new MigrationExtractResult(columns, rows, sourceEntity,
            DataHubMigrationCatalog.MapTargetEntity(Source, sourceEntity));
    }

    private static (string Soql, List<string> Columns) BuildSoql(
        string objectName, List<string> columns, DataHubMigrationImportMode mode, DateTime? sinceUtc)
    {
        var select = string.Join(",", columns);
        var soql = $"SELECT {select} FROM {objectName}";
        if (mode == DataHubMigrationImportMode.Delta && sinceUtc.HasValue)
            soql += $" WHERE LastModifiedDate > {sinceUtc.Value:yyyy-MM-ddTHH:mm:ssZ}";
        return (soql, columns);
    }

    private static string? FormatJsonValue(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString(),
        JsonValueKind.Number => v.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => null,
        _ => v.GetRawText()
    };
}

public sealed class HubSpotMigrationExtractor : IMigrationSourceExtractor
{
    private readonly IHttpClientFactory _http;
    private readonly IntegrationEndpointsOptions _endpoints;
    private readonly ILogger<HubSpotMigrationExtractor> _logger;

    public HubSpotMigrationExtractor(
        IHttpClientFactory http,
        IOptions<IntegrationEndpointsOptions> endpoints,
        ILogger<HubSpotMigrationExtractor> logger)
    {
        _http = http;
        _endpoints = endpoints.Value;
        _logger = logger;
    }

    public string Source => "HubSpot";

    public IReadOnlyList<DataHubMigrationEntityDto> SupportedEntities { get; } =
    [
        new("Companies", "Companies", "Customer", "HubSpot Company → Customer"),
        new("Contacts", "Contacts", "Lead", "HubSpot Contact → Lead"),
        new("Deals", "Deals", "Deal", "HubSpot Deal → Deal")
    ];

    public bool IsConfigured(TenantIntegrationConnectionSnapshot? connection)
        => connection != null && (
            !string.IsNullOrWhiteSpace(connection.AccessToken) ||
            MigrationConnectionHelper.GetSetting(connection, "apiKey") != null);

    public async Task<MigrationExtractResult> ExtractAsync(
        TenantIntegrationConnectionSnapshot connection, string sourceEntity,
        DataHubMigrationImportMode mode, DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        var (objectType, properties, columns) = sourceEntity switch
        {
            "Companies" => ("companies", "name,domain,phone,city,industry", new List<string> { "Name", "Company", "Phone", "City", "Industry" }),
            "Contacts" => ("contacts", "email,firstname,lastname,phone,company", new List<string> { "Email", "Name", "Phone", "Company" }),
            "Deals" => ("deals", "dealname,amount,dealstage,closedate", new List<string> { "Title", "Amount", "Stage", "CloseDate" }),
            _ => throw new ArgumentException($"Unsupported HubSpot entity: {sourceEntity}")
        };

        var token = connection.AccessToken ?? MigrationConnectionHelper.GetSetting(connection, "apiKey");
        if (string.IsNullOrWhiteSpace(token))
            throw new InvalidOperationException("HubSpot access token or apiKey required.");

        var client = _http.CreateClient("HubSpot");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        var baseUrl = _endpoints.HubSpotApiBase.TrimEnd('/');
        var rows = new List<Dictionary<string, string?>>();

        if (mode == DataHubMigrationImportMode.Delta && sinceUtc.HasValue)
        {
            await ExtractHubSpotDeltaViaSearchAsync(
                client, baseUrl, objectType, properties, sourceEntity, columns, sinceUtc.Value, rows, cancellationToken);
        }
        else
        {
            string? after = null;
            do
            {
                var url = $"{baseUrl}/crm/v3/objects/{objectType}?limit=100&properties={properties}";
                if (!string.IsNullOrEmpty(after)) url += $"&after={after}";
                var response = await client.GetAsync(url, cancellationToken);
                response.EnsureSuccessStatusCode();
                using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
                if (doc.RootElement.TryGetProperty("results", out var results))
                {
                    foreach (var item in results.EnumerateArray())
                    {
                        if (!item.TryGetProperty("properties", out var props)) continue;
                        rows.Add(MapHubSpotRow(sourceEntity, props, columns));
                    }
                }
                after = doc.RootElement.TryGetProperty("paging", out var paging) &&
                        paging.TryGetProperty("next", out var next) &&
                        next.TryGetProperty("after", out var afterEl)
                    ? afterEl.GetString()
                    : null;
            } while (!string.IsNullOrEmpty(after));
        }

        _logger.LogInformation("HubSpot migration extracted {Count} {Entity} rows", rows.Count, sourceEntity);
        return new MigrationExtractResult(columns, rows, sourceEntity,
            DataHubMigrationCatalog.MapTargetEntity(Source, sourceEntity));
    }

    private static async Task ExtractHubSpotDeltaViaSearchAsync(
        HttpClient client, string baseUrl, string objectType, string properties,
        string sourceEntity, List<string> columns, DateTime sinceUtc,
        List<Dictionary<string, string?>> rows, CancellationToken cancellationToken)
    {
        var sinceMs = new DateTimeOffset(sinceUtc).ToUnixTimeMilliseconds().ToString(CultureInfo.InvariantCulture);
        string? after = null;
        do
        {
            var body = new Dictionary<string, object>
            {
                ["limit"] = 100,
                ["properties"] = properties.Split(','),
                ["filterGroups"] = new[]
                {
                    new Dictionary<string, object>
                    {
                        ["filters"] = new[]
                        {
                            new Dictionary<string, object>
                            {
                                ["propertyName"] = "lastmodifieddate",
                                ["operator"] = "GT",
                                ["value"] = sinceMs
                            }
                        }
                    }
                }
            };
            if (!string.IsNullOrEmpty(after)) body["after"] = after;

            var response = await client.PostAsJsonAsync($"{baseUrl}/crm/v3/objects/{objectType}/search", body, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (doc.RootElement.TryGetProperty("results", out var results))
            {
                foreach (var item in results.EnumerateArray())
                {
                    if (!item.TryGetProperty("properties", out var props)) continue;
                    rows.Add(MapHubSpotRow(sourceEntity, props, columns));
                }
            }
            after = doc.RootElement.TryGetProperty("paging", out var paging) &&
                    paging.TryGetProperty("next", out var next) &&
                    next.TryGetProperty("after", out var afterEl)
                ? afterEl.GetString()
                : null;
        } while (!string.IsNullOrEmpty(after));
    }

    private static Dictionary<string, string?> MapHubSpotRow(string entity, JsonElement props, List<string> columns)
    {
        var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        if (entity == "Contacts")
        {
            var first = props.TryGetProperty("firstname", out var fn) ? fn.GetString() : "";
            var last = props.TryGetProperty("lastname", out var ln) ? ln.GetString() : "";
            row["Name"] = $"{first} {last}".Trim();
            row["Email"] = props.TryGetProperty("email", out var em) ? em.GetString() : null;
            row["Phone"] = props.TryGetProperty("phone", out var ph) ? ph.GetString() : null;
            row["Company"] = props.TryGetProperty("company", out var co) ? co.GetString() : null;
        }
        else if (entity == "Companies")
        {
            row["Name"] = props.TryGetProperty("name", out var n) ? n.GetString() : null;
            row["Company"] = row["Name"];
            row["Phone"] = props.TryGetProperty("phone", out var ph) ? ph.GetString() : null;
        }
        else
        {
            row["Title"] = props.TryGetProperty("dealname", out var t) ? t.GetString() : null;
            row["Amount"] = props.TryGetProperty("amount", out var a) ? a.GetString() : null;
            row["Stage"] = props.TryGetProperty("dealstage", out var s) ? s.GetString() : null;
        }
        return row;
    }
}

public sealed class DynamicsMigrationExtractor : IMigrationSourceExtractor
{
    private readonly IHttpClientFactory _http;
    private readonly IntegrationEndpointsOptions _endpoints;
    private readonly ILogger<DynamicsMigrationExtractor> _logger;

    public DynamicsMigrationExtractor(
        IHttpClientFactory http,
        IOptions<IntegrationEndpointsOptions> endpoints,
        ILogger<DynamicsMigrationExtractor> logger)
    {
        _http = http;
        _endpoints = endpoints.Value;
        _logger = logger;
    }

    public string Source => "Dynamics";

    public IReadOnlyList<DataHubMigrationEntityDto> SupportedEntities { get; } =
    [
        new("Accounts", "Accounts", "Customer", "Dynamics Account → Customer"),
        new("Contacts", "Contacts", "Customer", "Dynamics Contact → Customer"),
        new("Opportunities", "Opportunities", "Deal", "Dynamics Opportunity → Deal")
    ];

    public bool IsConfigured(TenantIntegrationConnectionSnapshot? connection)
        => connection != null
            && !string.IsNullOrWhiteSpace(connection.AccessToken)
            && !string.IsNullOrWhiteSpace(connection.InstanceUrl);

    public async Task<MigrationExtractResult> ExtractAsync(
        TenantIntegrationConnectionSnapshot connection, string sourceEntity,
        DataHubMigrationImportMode mode, DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        var (set, columns, map) = sourceEntity switch
        {
            "Accounts" => ("accounts", new List<string> { "Name", "Phone", "Company", "Email" },
                new Dictionary<string, string> { ["name"] = "Name", ["telephone1"] = "Phone", ["websiteurl"] = "Company", ["emailaddress1"] = "Email" }),
            "Contacts" => ("contacts", new List<string> { "Name", "Email", "Phone", "Company" },
                new Dictionary<string, string> { ["fullname"] = "Name", ["emailaddress1"] = "Email", ["telephone1"] = "Phone", ["parentcustomerid"] = "Company" }),
            "Opportunities" => ("opportunities", new List<string> { "Title", "Amount", "Stage" },
                new Dictionary<string, string> { ["name"] = "Title", ["estimatedvalue"] = "Amount", ["stepname"] = "Stage" }),
            _ => throw new ArgumentException($"Unsupported Dynamics entity: {sourceEntity}")
        };

        var client = _http.CreateClient("Dynamics");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connection.AccessToken);
        var select = string.Join(",", map.Keys);
        var url = $"{connection.InstanceUrl!.TrimEnd('/')}/api/data/{_endpoints.DynamicsApiVersion}/{set}?$select={select}&$top=5000";
        if (mode == DataHubMigrationImportMode.Delta && sinceUtc.HasValue)
            url += $"&$filter=modifiedon gt {sinceUtc.Value:yyyy-MM-ddTHH:mm:ssZ}";

        var rows = new List<Dictionary<string, string?>>();
        while (!string.IsNullOrEmpty(url))
        {
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (doc.RootElement.TryGetProperty("value", out var values))
            {
                foreach (var item in values.EnumerateArray())
                {
                    var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                    foreach (var (apiField, col) in map)
                        row[col] = item.TryGetProperty(apiField, out var v) ? FormatJsonValue(v) : null;
                    rows.Add(row);
                }
            }
            url = doc.RootElement.TryGetProperty("@odata.nextLink", out var next) ? next.GetString() : null;
        }

        _logger.LogInformation("Dynamics migration extracted {Count} {Entity} rows", rows.Count, sourceEntity);
        return new MigrationExtractResult(columns, rows, sourceEntity,
            DataHubMigrationCatalog.MapTargetEntity(Source, sourceEntity));
    }

    private static string? FormatJsonValue(JsonElement v) => v.ValueKind switch
    {
        JsonValueKind.String => v.GetString(),
        JsonValueKind.Number => v.GetRawText(),
        JsonValueKind.Null => null,
        _ => v.GetRawText()
    };
}

public sealed class ZohoMigrationExtractor : IMigrationSourceExtractor
{
    private readonly IHttpClientFactory _http;
    private readonly IntegrationEndpointsOptions _endpoints;
    private readonly ILogger<ZohoMigrationExtractor> _logger;

    public ZohoMigrationExtractor(
        IHttpClientFactory http,
        IOptions<IntegrationEndpointsOptions> endpoints,
        ILogger<ZohoMigrationExtractor> logger)
    {
        _http = http;
        _endpoints = endpoints.Value;
        _logger = logger;
    }

    public string Source => "Zoho";

    public IReadOnlyList<DataHubMigrationEntityDto> SupportedEntities { get; } =
    [
        new("Leads", "Leads", "Lead", "Zoho Lead → Lead"),
        new("Contacts", "Contacts", "Customer", "Zoho Contact → Customer"),
        new("Accounts", "Accounts", "Customer", "Zoho Account → Customer")
    ];

    public bool IsConfigured(TenantIntegrationConnectionSnapshot? connection)
        => connection != null && !string.IsNullOrWhiteSpace(connection.AccessToken);

    public async Task<MigrationExtractResult> ExtractAsync(
        TenantIntegrationConnectionSnapshot connection, string sourceEntity,
        DataHubMigrationImportMode mode, DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        var module = sourceEntity;
        var apiDomain = MigrationConnectionHelper.GetSetting(connection, "apiDomain")
            ?? _endpoints.ZohoApiBase.Replace("https://", "").TrimEnd('/');
        var baseUrl = apiDomain.StartsWith("http") ? apiDomain : $"https://{apiDomain}";
        var client = _http.CreateClient("Zoho");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Zoho-oauthtoken", connection.AccessToken);

        var rows = new List<Dictionary<string, string?>>();
        var page = 1;
        var columns = new List<string>();
        var hasMore = true;

        while (hasMore)
        {
            var url = $"{baseUrl.TrimEnd('/')}/crm/v2/{module}?page={page}&per_page=200";
            if (mode == DataHubMigrationImportMode.Delta && sinceUtc.HasValue)
                url += $"&modified_since={sinceUtc.Value:yyyy-MM-ddTHH:mm:ss}";

            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.GetArrayLength() == 0)
                break;

            foreach (var item in data.EnumerateArray())
            {
                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var prop in item.EnumerateObject())
                {
                    if (columns.Count < 20 && !columns.Contains(prop.Name, StringComparer.OrdinalIgnoreCase))
                        columns.Add(prop.Name);
                    row[prop.Name] = prop.Value.ValueKind == JsonValueKind.String ? prop.Value.GetString() : prop.Value.GetRawText();
                }
                NormalizeZohoRow(module, row);
                rows.Add(row);
            }

            hasMore = doc.RootElement.TryGetProperty("info", out var info) &&
                      info.TryGetProperty("more_records", out var more) && more.GetBoolean();
            page++;
            if (page > 500) break;
        }

        if (columns.Count == 0)
            columns = ["Name", "Email", "Phone", "Company"];

        _logger.LogInformation("Zoho migration extracted {Count} {Entity} rows", rows.Count, sourceEntity);
        return new MigrationExtractResult(columns, rows, sourceEntity,
            DataHubMigrationCatalog.MapTargetEntity(Source, sourceEntity));
    }

    private static void NormalizeZohoRow(string module, Dictionary<string, string?> row)
    {
        if (module == "Leads" || module == "Contacts")
        {
            if (!row.ContainsKey("Name") && row.TryGetValue("Full_Name", out var fn))
                row["Name"] = fn;
            if (!row.ContainsKey("Email") && row.TryGetValue("Email", out var em))
                row["Email"] = em;
        }
        if (module == "Accounts" && !row.ContainsKey("Company") && row.TryGetValue("Account_Name", out var an))
        {
            row["Name"] = an;
            row["Company"] = an;
        }
    }
}

public sealed class PipedriveMigrationExtractor : IMigrationSourceExtractor
{
    private readonly IHttpClientFactory _http;
    private readonly IntegrationEndpointsOptions _endpoints;
    private readonly ILogger<PipedriveMigrationExtractor> _logger;

    public PipedriveMigrationExtractor(
        IHttpClientFactory http,
        IOptions<IntegrationEndpointsOptions> endpoints,
        ILogger<PipedriveMigrationExtractor> logger)
    {
        _http = http;
        _endpoints = endpoints.Value;
        _logger = logger;
    }

    public string Source => "Pipedrive";

    public IReadOnlyList<DataHubMigrationEntityDto> SupportedEntities { get; } =
    [
        new("Organizations", "Organizations", "Customer", "Pipedrive Organization → Customer"),
        new("Persons", "Persons", "Lead", "Pipedrive Person → Lead"),
        new("Deals", "Deals", "Deal", "Pipedrive Deal → Deal")
    ];

    public bool IsConfigured(TenantIntegrationConnectionSnapshot? connection)
        => connection != null && (
            !string.IsNullOrWhiteSpace(MigrationConnectionHelper.GetSetting(connection, "apiToken")) ||
            !string.IsNullOrWhiteSpace(connection.AccessToken));

    public async Task<MigrationExtractResult> ExtractAsync(
        TenantIntegrationConnectionSnapshot connection, string sourceEntity,
        DataHubMigrationImportMode mode, DateTime? sinceUtc, CancellationToken cancellationToken = default)
    {
        var apiToken = MigrationConnectionHelper.GetSetting(connection, "apiToken") ?? connection.AccessToken
            ?? throw new InvalidOperationException("Pipedrive apiToken required in integration settings.");

        var (endpoint, columns, map) = sourceEntity switch
        {
            "Organizations" => ("organizations", new List<string> { "Name", "Company" },
                new Dictionary<string, string> { ["name"] = "Name" }),
            "Persons" => ("persons", new List<string> { "Name", "Email", "Phone" },
                new Dictionary<string, string> { ["name"] = "Name", ["email"] = "Email", ["phone"] = "Phone" }),
            "Deals" => ("deals", new List<string> { "Title", "Amount", "Stage" },
                new Dictionary<string, string> { ["title"] = "Title", ["value"] = "Amount", ["stage_id"] = "Stage" }),
            _ => throw new ArgumentException($"Unsupported Pipedrive entity: {sourceEntity}")
        };

        var client = _http.CreateClient("Pipedrive");
        var rows = new List<Dictionary<string, string?>>();
        var start = 0;
        const int limit = 500;
        var hasMore = true;
        var deltaMode = mode == DataHubMigrationImportMode.Delta && sinceUtc.HasValue;
        var sortParam = deltaMode ? "&sort=update_time DESC" : string.Empty;

        while (hasMore)
        {
            var url = $"{_endpoints.PipedriveApiBase.TrimEnd('/')}/{endpoint}?api_token={Uri.EscapeDataString(apiToken)}&start={start}&limit={limit}{sortParam}";
            var response = await client.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();
            using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (!doc.RootElement.TryGetProperty("data", out var data) || data.ValueKind != JsonValueKind.Array)
                break;

            var pageHasNewer = false;
            foreach (var item in data.EnumerateArray())
            {
                if (deltaMode &&
                    item.TryGetProperty("update_time", out var upd) &&
                    DateTime.TryParse(upd.GetString(), CultureInfo.InvariantCulture, DateTimeStyles.AdjustToUniversal, out var updated))
                {
                    if (updated <= sinceUtc!.Value)
                        continue;
                    pageHasNewer = true;
                }
                else if (deltaMode)
                {
                    pageHasNewer = true;
                }

                var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
                foreach (var (field, col) in map)
                {
                    if (item.TryGetProperty(field, out var v))
                    {
                        if (field == "email" && v.ValueKind == JsonValueKind.Array && v.GetArrayLength() > 0)
                            row[col] = v[0].TryGetProperty("value", out var ev) ? ev.GetString() : null;
                        else if (field == "phone" && v.ValueKind == JsonValueKind.Array && v.GetArrayLength() > 0)
                            row[col] = v[0].TryGetProperty("value", out var pv) ? pv.GetString() : null;
                        else
                            row[col] = v.ValueKind == JsonValueKind.String ? v.GetString() : v.GetRawText();
                    }
                }
                if (sourceEntity == "Organizations" && row.TryGetValue("Name", out var n))
                    row["Company"] = n;
                rows.Add(row);
            }

            if (deltaMode && !pageHasNewer)
                break;

            hasMore = doc.RootElement.TryGetProperty("additional_data", out var ad) &&
                      ad.TryGetProperty("pagination", out var pag) &&
                      pag.TryGetProperty("more_items_in_collection", out var more) && more.GetBoolean();
            start += limit;
            if (start > 50_000) break;
        }

        _logger.LogInformation("Pipedrive migration extracted {Count} {Entity} rows", rows.Count, sourceEntity);
        return new MigrationExtractResult(columns, rows, sourceEntity,
            DataHubMigrationCatalog.MapTargetEntity(Source, sourceEntity));
    }
}

public sealed class MigrationSourceExtractorRegistry
{
    private readonly IReadOnlyDictionary<string, IMigrationSourceExtractor> _extractors;

    public MigrationSourceExtractorRegistry(IEnumerable<IMigrationSourceExtractor> extractors)
        => _extractors = extractors.ToDictionary(e => e.Source, StringComparer.OrdinalIgnoreCase);

    public IMigrationSourceExtractor Get(string source) =>
        _extractors.TryGetValue(source, out var ext)
            ? ext
            : throw new ArgumentException($"Migration source '{source}' is not supported.");

    public IReadOnlyList<IMigrationSourceExtractor> All => _extractors.Values.ToList();
}
