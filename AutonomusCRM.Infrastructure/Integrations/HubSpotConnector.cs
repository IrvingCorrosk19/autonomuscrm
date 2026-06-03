using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class HubSpotConnector : IntegrationConnectorBase
{
    public HubSpotConnector(
        ITenantIntegrationRepository connections,
        ICrmImportService import,
        ICustomerRepository customers,
        IHttpClientFactory httpClientFactory,
        ILogger<HubSpotConnector> logger)
        : base(connections, import, customers, httpClientFactory, logger) { }

    public override string Provider => IntegrationProviders.HubSpot;

    protected override async Task<IReadOnlyList<CustomerImportRow>> PullExternalRecordsAsync(
        TenantIntegrationConnection conn, CancellationToken cancellationToken)
    {
        var token = conn.AccessToken ?? GetSetting(conn, "apiKey");
        if (string.IsNullOrWhiteSpace(token)) return Array.Empty<CustomerImportRow>();

        var client = HttpClientFactory.CreateClient("HubSpot");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync(
            "https://api.hubapi.com/crm/v3/objects/contacts?limit=100&properties=email,firstname,lastname,phone,company",
            cancellationToken);
        if (!response.IsSuccessStatusCode) return Array.Empty<CustomerImportRow>();

        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
        var rows = new List<CustomerImportRow>();
        if (!doc.RootElement.TryGetProperty("results", out var results)) return rows;

        foreach (var item in results.EnumerateArray())
        {
            if (!item.TryGetProperty("properties", out var props)) continue;
            var first = props.TryGetProperty("firstname", out var fn) ? fn.GetString() : "";
            var last = props.TryGetProperty("lastname", out var ln) ? ln.GetString() : "";
            var name = $"{first} {last}".Trim();
            if (string.IsNullOrWhiteSpace(name)) name = "HubSpot Contact";
            rows.Add(new CustomerImportRow(
                name,
                props.TryGetProperty("email", out var em) ? em.GetString() : null,
                props.TryGetProperty("phone", out var ph) ? ph.GetString() : null,
                props.TryGetProperty("company", out var co) ? co.GetString() : null));
        }

        return rows;
    }

    protected override async Task<int> PushLocalChangesAsync(
        TenantIntegrationConnection conn, Guid tenantId, CancellationToken cancellationToken)
    {
        var token = conn.AccessToken ?? GetSetting(conn, "apiKey");
        if (string.IsNullOrWhiteSpace(token)) return 0;

        var local = await Customers.GetAllAsync(cancellationToken);
        var client = HttpClientFactory.CreateClient("HubSpot");
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var pushed = 0;
        foreach (var c in local.Take(20))
        {
            if (string.IsNullOrWhiteSpace(c.Email)) continue;
            var payload = JsonSerializer.Serialize(new
            {
                properties = new
                {
                    email = c.Email,
                    firstname = c.Name.Split(' ').FirstOrDefault(),
                    lastname = string.Join(' ', c.Name.Split(' ').Skip(1)),
                    phone = c.Phone,
                    company = c.Company
                }
            });
            var content = new StringContent(payload, System.Text.Encoding.UTF8, "application/json");
            var res = await client.PostAsync("https://api.hubapi.com/crm/v3/objects/contacts", content, cancellationToken);
            if (res.IsSuccessStatusCode) pushed++;
        }

        return pushed;
    }
}
