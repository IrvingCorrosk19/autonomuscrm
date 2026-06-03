using System.Net.Http.Headers;
using System.Text.Json;
using AutonomusCRM.Application.Common.Imports;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.Integrations;

public abstract class IntegrationConnectorBase : IIntegrationConnector
{
    protected readonly ITenantIntegrationRepository Connections;
    protected readonly ICrmImportService Import;
    protected readonly ICustomerRepository Customers;
    protected readonly IHttpClientFactory HttpClientFactory;
    protected readonly ILogger Logger;

    protected IntegrationConnectorBase(
        ITenantIntegrationRepository connections,
        ICrmImportService import,
        ICustomerRepository customers,
        IHttpClientFactory httpClientFactory,
        ILogger logger)
    {
        Connections = connections;
        Import = import;
        Customers = customers;
        HttpClientFactory = httpClientFactory;
        Logger = logger;
    }

    public abstract string Provider { get; }

    public async Task<IntegrationSyncResultDto> SyncBidirectionalAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var conn = await Connections.GetAsync(tenantId, Provider, cancellationToken);
        if (conn == null || !conn.IsEnabled)
            return new IntegrationSyncResultDto(Provider, 0, 0, 1, new[] { "Integration not connected" });

        var messages = new List<string>();
        var pulled = 0;
        var pushed = 0;
        var errors = 0;

        try
        {
            var pullRecords = await PullExternalRecordsAsync(conn, cancellationToken);
            if (pullRecords.Count > 0)
            {
                var result = await Import.ImportCustomersAsync(tenantId, pullRecords, cancellationToken);
                pulled = result.Created;
                messages.Add($"Imported {result.Created} customers, failed {result.Failed}");
            }

            pushed = await PushLocalChangesAsync(conn, tenantId, cancellationToken);
            conn.MarkSync("OK");
            await Connections.UpsertAsync(conn, cancellationToken);
        }
        catch (Exception ex)
        {
            errors++;
            messages.Add(ex.Message);
            conn.MarkSync($"Error: {ex.Message}");
            await Connections.UpsertAsync(conn, cancellationToken);
            Logger.LogError(ex, "{Provider} sync failed for tenant {TenantId}", Provider, tenantId);
        }

        return new IntegrationSyncResultDto(Provider, pulled, pushed, errors, messages);
    }

    protected abstract Task<IReadOnlyList<CustomerImportRow>> PullExternalRecordsAsync(
        TenantIntegrationConnection conn, CancellationToken cancellationToken);

    protected abstract Task<int> PushLocalChangesAsync(
        TenantIntegrationConnection conn, Guid tenantId, CancellationToken cancellationToken);

    protected static HttpClient CreateAuthedClient(string? token, string baseAddress)
    {
        var client = new HttpClient { BaseAddress = new Uri(baseAddress) };
        if (!string.IsNullOrWhiteSpace(token))
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    protected static string? GetSetting(TenantIntegrationConnection conn, string key)
        => conn.Settings.TryGetValue(key, out var v) ? v : null;
}
