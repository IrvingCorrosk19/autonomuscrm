using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class SalesforceConnector : IntegrationConnectorBase
{
    public SalesforceConnector(
        ITenantIntegrationRepository connections,
        ICrmImportService import,
        ICustomerRepository customers,
        IHttpClientFactory httpClientFactory,
        ILogger<SalesforceConnector> logger)
        : base(connections, import, customers, httpClientFactory, logger) { }

    public override string Provider => IntegrationProviders.Salesforce;

    protected override async Task<IReadOnlyList<CustomerImportRow>> PullExternalRecordsAsync(
        TenantIntegrationConnection conn, CancellationToken cancellationToken)
    {
        var token = conn.AccessToken;
        var instance = conn.InstanceUrl ?? GetSetting(conn, "instanceUrl");
        if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(instance)) return Array.Empty<CustomerImportRow>();

        var client = HttpClientFactory.CreateClient("Salesforce");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var url = $"{instance.TrimEnd('/')}/services/data/v59.0/query?q=" +
                  Uri.EscapeDataString("SELECT Id,Name,Email,Phone FROM Contact LIMIT 100");
        var response = await client.GetAsync(url, cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<CustomerImportRow>();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var rows = new List<CustomerImportRow>();
        if (!doc.RootElement.TryGetProperty("records", out var records)) return rows;

        foreach (var item in records.EnumerateArray())
        {
            rows.Add(new CustomerImportRow(
                item.TryGetProperty("Name", out var n) ? n.GetString() ?? "SF Contact" : "SF Contact",
                item.TryGetProperty("Email", out var e) ? e.GetString() : null,
                item.TryGetProperty("Phone", out var p) ? p.GetString() : null,
                null));
        }

        return rows;
    }

    protected override Task<int> PushLocalChangesAsync(
        TenantIntegrationConnection conn, Guid tenantId, CancellationToken cancellationToken)
        => Task.FromResult(0);
}
