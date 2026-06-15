using System.Net;
using System.Net.Http.Json;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

internal static class DipIntegrationTestHelpers
{
    internal static async Task<Guid> EnsureConnectionAsync(HttpClient client, Guid tenantId, string? name = null)
    {
        var list = await client.GetAsync($"/api/db-intelligence/connections?tenantId={tenantId}");
        if (list.StatusCode == HttpStatusCode.OK)
        {
            var existing = await list.Content.ReadFromJsonAsync<List<DbConnectionProfileDto>>();
            var reusable = existing?
                .Where(c => c.IsActive && c.LastTestSucceeded == true)
                .OrderByDescending(c => c.LastTestedAtUtc ?? c.CreatedAtUtc)
                .FirstOrDefault();
            if (reusable != null)
                return reusable.Id;
        }

        var create = await client.PostAsJsonAsync($"/api/db-intelligence/connections?tenantId={tenantId}",
            new CreateDbConnectionProfileRequest(
                name ?? $"DIP Test {Guid.NewGuid():N}",
                DbEngineType.PostgreSQL, "127.0.0.1", 5432, "autonomuscrm", "postgres", "Panama2020$", true));
        if (create.StatusCode != HttpStatusCode.OK)
        {
            var body = await create.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Could not ensure DIP connection ({create.StatusCode}): {body}");
        }

        return (await create.Content.ReadFromJsonAsync<DbConnectionProfileDto>())!.Id;
    }
}
