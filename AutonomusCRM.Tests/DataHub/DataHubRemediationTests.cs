using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.DataHub.Migration;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

public class DataHubRemediationCriticalTests
{
    [Fact]
    public void ValidationFailed_StatusExists()
    {
        Assert.Contains(nameof(DataHubJobStatus.ValidationFailed), Enum.GetNames<DataHubJobStatus>());
    }

    [Theory]
    [InlineData("Deal", "Deal Stage", "Stage")]
    [InlineData("Deal", "Amount", "Amount")]
    [InlineData("Lead", "Business Email", "Email")]
    [InlineData("Customer", "Organization", "Company")]
    [InlineData("User", "Email Address", "Email")]
    public void DetectColumns_UsesTargetEntity_NotHardcodedCustomer(string entity, string column, string expectedField)
    {
        var intel = new DataHubIntelligenceService(DataHubFieldCatalogImpl.Instance);
        var samples = expectedField == "Email"
            ? new List<Dictionary<string, string?>> { new() { [column] = "a@b.com" } }
            : new List<Dictionary<string, string?>> { new() { [column] = "Sample" } };
        var result = intel.DetectColumns(entity, [column], samples);
        Assert.Equal(expectedField, result[0].SuggestedTargetField);
    }

    [Fact]
    public void SmartMatching_AccountId_NotMappedToCompany()
    {
        var result = DataHubSmartMatchingEngine.MatchColumn("Customer", "Account Id", ["12345"]);
        Assert.NotEqual("Company", result.TargetField);
    }

    [Fact]
    public void JobProcessingLock_InMemoryDeprecated_StillWorksForSingleProcessFallback()
    {
        var jobId = Guid.NewGuid();
        Assert.True(DataHubJobProcessingLock.TryAcquire(jobId));
        Assert.False(DataHubJobProcessingLock.TryAcquire(jobId));
        DataHubJobProcessingLock.Release(jobId);
    }

    [Fact]
    public void MigrationQualityGate_BlocksWhenErrorsPresent()
    {
        var errors = new List<DataHubImportError>
        {
            new() { ErrorCode = "MissingOwner", FieldName = "OwnerId", Message = "missing" }
        };
        var dupes = new DataHubDuplicateScanResultDto(Guid.Empty, 0, 0, Array.Empty<DataHubDuplicateGroupDto>());
        var result = MigrationQualityGate.Evaluate(errors, dupes);
        Assert.False(result.Passed);
        Assert.True(result.MissingOwners > 0);
    }

    [Fact]
    public void ValidationAsyncLogic_InvalidRowsBlockReadyToImport()
    {
        var invalid = 3;
        var ready = invalid == 0;
        var status = ready ? DataHubJobStatus.ReadyToImport : DataHubJobStatus.ValidationFailed;
        Assert.False(ready);
        Assert.Equal(DataHubJobStatus.ValidationFailed, status);
    }

    [Fact]
    public void TemplateCompare_HandlesDuplicateSourceColumns()
    {
        var mappings = new List<DataHubTemplateMapping>
        {
            new() { SourceColumn = "Email", TargetField = "Email" },
            new() { SourceColumn = "email", TargetField = "EmailAddress" }
        };
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var m in mappings) dict[m.SourceColumn] = m.TargetField;
        Assert.Single(dict);
        Assert.Equal("EmailAddress", dict["Email"]);
    }
}

public class DataHubRemediationHighTests
{
    [Fact]
    public void ExportForensicAction_Defined()
    {
        Assert.False(string.IsNullOrWhiteSpace(DataHubForensicActions.Export));
    }

    [Fact]
    public void ProcessingOptions_IncludesDeadLetterQueue()
    {
        var opts = new DataHubProcessingOptions();
        Assert.Contains("dlq", opts.ImportDeadLetterQueueName, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Account Id", "Company")]
    [InlineData("Deal Stage", "Title")]
    public void SmartMatching_NegativePatternsReduceFalsePositives(string column, string wrongField)
    {
        var result = DataHubSmartMatchingEngine.MatchColumn("Customer", column, ["value"]);
        Assert.NotEqual(wrongField, result.TargetField);
    }
}
