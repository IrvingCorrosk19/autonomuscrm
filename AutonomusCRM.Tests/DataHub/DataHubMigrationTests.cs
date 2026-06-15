using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Infrastructure.DataHub.Migration;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

internal sealed class MigrationTestHttpClientFactory : IHttpClientFactory
{
    public HttpClient CreateClient(string name) => new HttpClient();
}

public class DataHubMigrationCatalogTests
{
    [Theory]
    [InlineData("Salesforce", "Leads", "Lead")]
    [InlineData("HubSpot", "Deals", "Deal")]
    [InlineData("Dynamics", "Accounts", "Customer")]
    [InlineData("Zoho", "Contacts", "Customer")]
    [InlineData("Pipedrive", "Organizations", "Customer")]
    public void MapTargetEntity_MapsAllSupportedSources(string source, string entity, string expected)
    {
        Assert.Equal(expected, DataHubMigrationCatalog.MapTargetEntity(source, entity));
    }

    [Fact]
    public void SupportedSources_IncludesFiveCrms()
    {
        Assert.Equal(5, DataHubMigrationCatalog.SupportedSources.Count);
        Assert.Contains("Salesforce", DataHubMigrationCatalog.SupportedSources);
        Assert.Contains("Pipedrive", DataHubMigrationCatalog.SupportedSources);
    }
}

public class MigrationCsvBuilderTests
{
    [Fact]
    public void ToCsvStream_ProducesValidHeaderAndRows()
    {
        var columns = new[] { "Name", "Email" };
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["Name"] = "Acme", ["Email"] = "a@acme.com" },
            new() { ["Name"] = "Beta", ["Email"] = "b@beta.com" }
        };
        using var ms = MigrationCsvBuilder.ToCsvStream(columns, rows);
        using var reader = new StreamReader(ms);
        var text = reader.ReadToEnd();
        Assert.Contains("Name,Email", text);
        Assert.Contains("Acme,a@acme.com", text);
    }

    [Fact]
    public void ToCsvStream_EscapesCommasInValues()
    {
        var columns = new[] { "Name" };
        var rows = new List<Dictionary<string, string?>> { new() { ["Name"] = "Smith, Inc" } };
        using var ms = MigrationCsvBuilder.ToCsvStream(columns, rows);
        var text = new StreamReader(ms).ReadToEnd();
        Assert.Contains("\"Smith, Inc\"", text);
    }
}

public class MigrationExtractorRegistryTests
{
    [Fact]
    public void Registry_ResolvesAllFiveSources()
    {
        IMigrationSourceExtractor[] extractors =
        [
            new SalesforceMigrationExtractor(new MigrationTestHttpClientFactory(), NullLogger<SalesforceMigrationExtractor>.Instance),
            new HubSpotMigrationExtractor(new MigrationTestHttpClientFactory(), Options.Create(new IntegrationEndpointsOptions()), NullLogger<HubSpotMigrationExtractor>.Instance),
            new DynamicsMigrationExtractor(new MigrationTestHttpClientFactory(), Options.Create(new IntegrationEndpointsOptions()), NullLogger<DynamicsMigrationExtractor>.Instance),
            new ZohoMigrationExtractor(new MigrationTestHttpClientFactory(), Options.Create(new IntegrationEndpointsOptions()), NullLogger<ZohoMigrationExtractor>.Instance),
            new PipedriveMigrationExtractor(new MigrationTestHttpClientFactory(), Options.Create(new IntegrationEndpointsOptions()), NullLogger<PipedriveMigrationExtractor>.Instance)
        ];
        var registry = new MigrationSourceExtractorRegistry(extractors);
        foreach (var source in DataHubMigrationCatalog.SupportedSources)
        {
            var ext = registry.Get(source);
            Assert.Equal(source, ext.Source);
            Assert.NotEmpty(ext.SupportedEntities);
        }
    }
}

public class MigrationConnectionHelperTests
{
    [Fact]
    public void IsConfigured_Salesforce_RequiresTokenAndInstance()
    {
        var ext = new SalesforceMigrationExtractor(
            new MigrationTestHttpClientFactory(),
            NullLogger<SalesforceMigrationExtractor>.Instance);
        Assert.False(ext.IsConfigured(new TenantIntegrationConnectionSnapshot(
            Guid.NewGuid(), "Salesforce", null, null, null, new Dictionary<string, string>(), null)));
        Assert.True(ext.IsConfigured(new TenantIntegrationConnectionSnapshot(
            Guid.NewGuid(), "Salesforce", "token", null, "https://example.my.salesforce.com", new Dictionary<string, string>(), null)));
    }
}
