using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class StripeDataConnector : IntegrationConnectorBase
{
    public StripeDataConnector(
        ITenantIntegrationRepository connections,
        ICrmImportService import,
        ICustomerRepository customers,
        IHttpClientFactory httpClientFactory,
        ILogger<StripeDataConnector> logger)
        : base(connections, import, customers, httpClientFactory, logger) { }

    public override string Provider => IntegrationProviders.Stripe;

    protected override async Task<IReadOnlyList<CustomerImportRow>> PullExternalRecordsAsync(
        TenantIntegrationConnection conn, CancellationToken cancellationToken)
    {
        var key = conn.AccessToken ?? GetSetting(conn, "secretKey");
        if (string.IsNullOrWhiteSpace(key)) return Array.Empty<CustomerImportRow>();

        var client = HttpClientFactory.CreateClient("StripeData");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", key);

        var response = await client.GetAsync("https://api.stripe.com/v1/customers?limit=100", cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<CustomerImportRow>();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        if (!doc.RootElement.TryGetProperty("data", out var data)) return Array.Empty<CustomerImportRow>();

        var rows = new List<CustomerImportRow>();
        foreach (var c in data.EnumerateArray())
        {
            var name = c.TryGetProperty("name", out var n) ? n.GetString() : null;
            var email = c.TryGetProperty("email", out var e) ? e.GetString() : null;
            if (string.IsNullOrWhiteSpace(email)) continue;
            rows.Add(new CustomerImportRow(name ?? email!, email, null, null));
        }

        return rows;
    }

    protected override Task<int> PushLocalChangesAsync(
        TenantIntegrationConnection conn, Guid tenantId, CancellationToken cancellationToken)
        => Task.FromResult(0);
}
