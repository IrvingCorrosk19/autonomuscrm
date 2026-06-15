using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

public class DataHubExtractServiceTests
{
    [Fact]
    public void ParseCsvLine_HandlesQuotedCommas()
    {
        var fields = DataHubExtractService.ParseCsvLine("\"Smith, John\",test@email.com", ",");
        Assert.Equal(2, fields.Count);
        Assert.Equal("Smith, John", fields[0]);
        Assert.Equal("test@email.com", fields[1]);
    }

    [Fact]
    public void DetectDelimiter_PrefersSemicolonForEuropeanCsv()
    {
        var sample = "Name;Email;Phone\nJohn;j@x.com;123";
        Assert.Equal(";", DataHubExtractService.DetectDelimiter(sample));
    }
}

public class DataHubTransformServiceTests
{
    private readonly DataHubTransformService _svc = new();

    [Fact]
    public void NormalizeEmail_LowercasesAndTrims()
    {
        var result = _svc.ApplyTransform("  Test@Example.COM  ", nameof(DataHubTransformType.NormalizeEmail));
        Assert.Equal("test@example.com", result);
    }

    [Fact]
    public void Trim_RemovesWhitespace()
    {
        Assert.Equal("hello", _svc.ApplyTransform("  hello  ", nameof(DataHubTransformType.Trim)));
    }
}

public class DataHubSecurityServiceTests
{
    private readonly DataHubSecurityService _svc = new();

    [Fact]
    public void ValidateUpload_RejectsPathTraversal()
    {
        var (ok, error) = _svc.ValidateUpload("../etc/passwd.csv", 100, "text/csv");
        Assert.False(ok);
        Assert.Contains("Invalid", error);
    }

    [Fact]
    public void ValidateUpload_AcceptsValidCsv()
    {
        var (ok, _) = _svc.ValidateUpload("leads.csv", 1024, "text/csv");
        Assert.True(ok);
    }

    [Fact]
    public void SanitizeCellValue_PreventsCsvInjection()
    {
        Assert.StartsWith("'", _svc.SanitizeCellValue("=1+1"));
        Assert.StartsWith("'", _svc.SanitizeCellValue("+1234567890"));
    }
}

public class DataHubFieldCatalogTests
{
    [Fact]
    public void AutoMap_MatchesEmailSynonyms()
    {
        var catalog = DataHubFieldCatalogImpl.Instance;
        var result = catalog.SuggestMappings("Customer", ["correo", "nombre", "telefono"]);
        Assert.Contains(result.Mappings, m => m.TargetField == "Email");
        Assert.Contains(result.Mappings, m => m.TargetField == "Name");
    }

    [Fact]
    public void GetFields_CustomerHasRequiredName()
    {
        var fields = DataHubFieldCatalogImpl.Instance.GetFields("Customer");
        Assert.Contains(fields, f => f.Name == "Name" && f.IsRequired);
    }
}

public class DataHubConstantsTests
{
    [Fact]
    public void MaxFileBytes_AllowsLargeEnterpriseImports()
    {
        Assert.True(DataHubConstants.MaxFileBytes >= 50 * 1024 * 1024);
    }

    [Fact]
    public void AllowedExtensions_IncludesExcel()
    {
        Assert.Contains(".xlsx", DataHubConstants.AllowedExtensions);
    }
}

public class DataHubIntelligenceTests
{
    [Fact]
    public void DetectColumns_EmailColumn_HighConfidence()
    {
        var catalog = DataHubFieldCatalogImpl.Instance;
        var intel = new DataHubIntelligenceService(catalog);
        var cols = intel.DetectColumns("Customer", ["Email Address"], [new Dictionary<string, string?> { ["Email Address"] = "john@test.com" }]);
        Assert.Single(cols);
        Assert.Equal("Email", cols[0].DetectedType);
        Assert.True(cols[0].ConfidencePercent >= 80);
    }

    [Fact]
    public void AnalyzeFile_SuggestsLeadWhenSourcePresent()
    {
        var catalog = DataHubFieldCatalogImpl.Instance;
        var intel = new DataHubIntelligenceService(catalog);
        var rows = new List<Dictionary<string, string?>> { new() { ["Name"] = "A", ["Source"] = "Web", ["Email"] = "a@t.com" } };
        var result = intel.AnalyzeFile("leads.csv", ["Name", "Source", "Email"], rows);
        Assert.Equal("Lead", result.SuggestedTargetEntity);
        Assert.True(result.OverallConfidencePercent >= 55);
    }
}

public class DataHubRulesEngineTests
{
    [Fact]
    public void ApplyRules_SetsDefaultSourceWhenEmpty()
    {
        var engine = new DataHubRulesEngineService(null!);
        var rules = engine.GetDefaultRules("Lead");
        var row = new Dictionary<string, string?> { ["Source"] = "" };
        var result = engine.ApplyRules(row, rules);
        Assert.Equal("Other", result["Source"]);
    }
}
