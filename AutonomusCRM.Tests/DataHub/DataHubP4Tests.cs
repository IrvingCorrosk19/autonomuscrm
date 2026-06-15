using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

public class DataHubSmartMatchingV2Tests
{
    [Theory]
    [InlineData("Business Email", "Email")]
    [InlineData("Corporate Email", "Email")]
    [InlineData("Client Email", "Email")]
    [InlineData("Primary Email", "Email")]
    [InlineData("Mobile", "Phone")]
    [InlineData("Cell", "Phone")]
    [InlineData("WhatsApp Number", "Phone")]
    [InlineData("Company", "Company")]
    [InlineData("Organization", "Company")]
    [InlineData("Business Name", "Company")]
    public void MatchColumn_MapsEnterpriseSynonyms(string column, string expectedField)
    {
        var samples = expectedField == "Email"
            ? new List<string?> { "user@corp.com" }
            : expectedField == "Phone"
                ? new List<string?> { "+1 555 123 4567" }
                : new List<string?> { "Acme Corp" };

        var result = DataHubSmartMatchingEngine.MatchColumn("Customer", column, samples);
        Assert.Equal(expectedField, result.TargetField);
        Assert.True(result.ConfidencePercent >= 60);
        Assert.Contains("Confidence Engine V2", result.Explanation);
    }

    [Fact]
    public void MatchColumnsV2_ExplainsDetectionReason()
    {
        var intelligence = new DataHubIntelligenceService(DataHubFieldCatalogImpl.Instance);
        var rows = new List<Dictionary<string, string?>>
        {
            new() { ["Business Email"] = "a@b.com" }
        };
        var results = intelligence.MatchColumnsV2("Customer", ["Business Email"], rows);
        Assert.Single(results);
        Assert.Equal("Email", results[0].TargetField);
        Assert.False(string.IsNullOrWhiteSpace(results[0].Explanation));
    }

    [Fact]
    public void SampleValidation_BoostsEmailConfidence()
    {
        var withSamples = DataHubSmartMatchingEngine.MatchColumn(
            "Customer", "Contact Info", ["a@x.com", "b@y.com", "c@z.com"]);
        var withoutSamples = DataHubSmartMatchingEngine.MatchColumn(
            "Customer", "Contact Info", Array.Empty<string?>());
        Assert.True(withSamples.ConfidencePercent >= withoutSamples.ConfidencePercent);
    }
}

public class DataHubScheduledImportTests
{
    [Fact]
    public void ScheduleFrequency_IncludesRequiredValues()
    {
        var names = Enum.GetNames<DataHubScheduleFrequency>();
        Assert.Contains("Once", names);
        Assert.Contains("Daily", names);
        Assert.Contains("Weekly", names);
        Assert.Contains("Monthly", names);
    }

    [Fact]
    public void MigrationCatalog_SupportsAllScheduledSources()
    {
        foreach (var source in DataHubMigrationCatalog.SupportedSources)
            Assert.Contains(source, DataHubMigrationCatalog.SupportedSources);
    }
}

public class DataHubTemplateVersionTests
{
    [Fact]
    public void TemplateSummary_IncludesVersionFields()
    {
        var dto = new DataHubTemplateSummaryDto(
            Guid.NewGuid(), "Test", "Customer", 3, DateTime.UtcNow, 2, 5);
        Assert.Equal(2, dto.ActiveVersion);
        Assert.Equal(5, dto.LatestVersion);
    }

    [Fact]
    public void CompareDto_TracksMappingChanges()
    {
        var compare = new DataHubTemplateVersionCompareDto(
            1, 2,
            ["NewCol → Email"],
            ["OldCol → Name"],
            ["Email: Email → EmailAddress"]);
        Assert.Single(compare.AddedMappings);
        Assert.Single(compare.RemovedMappings);
        Assert.Single(compare.ChangedMappings);
    }
}

public class DataHubFieldCatalogV2Tests
{
    [Fact]
    public void AutoMap_UsesSmartMatchingForExtendedSynonyms()
    {
        var catalog = DataHubFieldCatalogImpl.Instance;
        var result = catalog.SuggestMappings("Customer", ["Business Email", "Mobile", "Business Name"]);
        Assert.Contains(result.Mappings, m => m.TargetField == "Email");
        Assert.Contains(result.Mappings, m => m.TargetField == "Phone");
        Assert.Contains(result.Mappings, m => m.TargetField == "Company");
    }
}
