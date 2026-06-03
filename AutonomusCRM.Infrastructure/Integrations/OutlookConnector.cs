using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class OutlookConnector : IntegrationConnectorBase
{
    public OutlookConnector(
        ITenantIntegrationRepository connections,
        ICrmImportService import,
        ICustomerRepository customers,
        IHttpClientFactory httpClientFactory,
        ILogger<OutlookConnector> logger)
        : base(connections, import, customers, httpClientFactory, logger) { }

    public override string Provider => IntegrationProviders.Outlook;

    protected override async Task<IReadOnlyList<CustomerImportRow>> PullExternalRecordsAsync(
        TenantIntegrationConnection conn, CancellationToken cancellationToken)
    {
        var token = conn.AccessToken;
        if (string.IsNullOrWhiteSpace(token)) return Array.Empty<CustomerImportRow>();

        var client = HttpClientFactory.CreateClient("Outlook");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            "https://graph.microsoft.com/v1.0/me/messages?$top=50&$select=from,subject", cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<CustomerImportRow>();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("value", out var value)) return Array.Empty<CustomerImportRow>();

        var rows = new List<CustomerImportRow>();
        foreach (var msg in value.EnumerateArray())
        {
            if (!msg.TryGetProperty("from", out var from) ||
                !from.TryGetProperty("emailAddress", out var ea) ||
                !ea.TryGetProperty("address", out var addr)) continue;
            var email = addr.GetString();
            if (string.IsNullOrWhiteSpace(email)) continue;
            var name = ea.TryGetProperty("name", out var n) ? n.GetString() : email.Split('@')[0];
            rows.Add(new CustomerImportRow(name ?? "Outlook Contact", email, null, null));
        }

        return rows.DistinctBy(r => r.Email).ToList();
    }

    protected override Task<int> PushLocalChangesAsync(
        TenantIntegrationConnection conn, Guid tenantId, CancellationToken cancellationToken)
        => Task.FromResult(0);
}
