using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class GmailConnector : IntegrationConnectorBase
{
    public GmailConnector(
        ITenantIntegrationRepository connections,
        ICrmImportService import,
        ICustomerRepository customers,
        IHttpClientFactory httpClientFactory,
        ILogger<GmailConnector> logger)
        : base(connections, import, customers, httpClientFactory, logger) { }

    public override string Provider => IntegrationProviders.Gmail;

    protected override async Task<IReadOnlyList<CustomerImportRow>> PullExternalRecordsAsync(
        TenantIntegrationConnection conn, CancellationToken cancellationToken)
    {
        var token = conn.AccessToken;
        if (string.IsNullOrWhiteSpace(token)) return Array.Empty<CustomerImportRow>();

        var client = HttpClientFactory.CreateClient("Gmail");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var listRes = await client.GetAsync(
            "https://gmail.googleapis.com/gmail/v1/users/me/messages?maxResults=50", cancellationToken);
        if (!listRes.IsSuccessStatusCode) return Array.Empty<CustomerImportRow>();

        using var listDoc = JsonDocument.Parse(await listRes.Content.ReadAsStringAsync(cancellationToken));
        if (!listDoc.RootElement.TryGetProperty("messages", out var messages)) return Array.Empty<CustomerImportRow>();

        var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var msg in messages.EnumerateArray())
        {
            if (!msg.TryGetProperty("id", out var idEl)) continue;
            var detail = await client.GetAsync(
                $"https://gmail.googleapis.com/gmail/v1/users/me/messages/{idEl.GetString()}?format=metadata&metadataHeaders=From",
                cancellationToken);
            if (!detail.IsSuccessStatusCode) continue;
            using var detailDoc = JsonDocument.Parse(await detail.Content.ReadAsStringAsync(cancellationToken));
            if (!detailDoc.RootElement.TryGetProperty("payload", out var payload)) continue;
            if (!payload.TryGetProperty("headers", out var headers)) continue;
            foreach (var h in headers.EnumerateArray())
            {
                if (h.TryGetProperty("name", out var name) && name.GetString() == "From" &&
                    h.TryGetProperty("value", out var val))
                {
                    var from = val.GetString() ?? "";
                    var email = ExtractEmail(from);
                    if (!string.IsNullOrWhiteSpace(email)) emails.Add(email);
                }
            }
        }

        return emails.Select(e => new CustomerImportRow(e.Split('@')[0], e, null, null)).ToList();
    }

    protected override Task<int> PushLocalChangesAsync(
        TenantIntegrationConnection conn, Guid tenantId, CancellationToken cancellationToken)
        => Task.FromResult(0);

    private static string? ExtractEmail(string from)
    {
        var start = from.IndexOf('<');
        var end = from.IndexOf('>');
        if (start >= 0 && end > start) return from[(start + 1)..end].Trim();
        return from.Contains('@') ? from.Trim() : null;
    }
}
