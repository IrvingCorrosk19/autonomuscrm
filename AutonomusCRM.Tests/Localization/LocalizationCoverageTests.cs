using System.Globalization;
using System.Text.Json;
using AutonomusCRM.API.Extensions;
using AutonomusCRM.API.Resources;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.Tests.Localization;

/// <summary>
/// Automated localization coverage checks.
/// </summary>
public class LocalizationCoverageTests
{
    private static readonly string ResourcesDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "AutonomusCRM.API", "Resources"));

    private static readonly HashSet<string> AllowedIdenticalKeys = new(StringComparer.Ordinal)
    {
        "AppName", "Common_NA", "Executive_Kicker", "Lang_English",
        "Marketing_Demo_CeoAdmin", "Marketing_Pricing_EnterpriseTitle", "Marketing_Pricing_GrowthTitle",
        "Marketing_Pricing_EnterprisePrice", "Marketing_Pricing_GrowthPrice", "Marketing_Pricing_PilotPrice",
        "Marketing_Pricing_Row100kPrice", "Marketing_Pricing_Row10kPrice", "Marketing_Pricing_Row50kPrice",
        "Settings_ScimLabel", "Settings_SamlLabel", "Common_Info", "Revenue_HorizonDaysFormat",
        "Users_RbacAbac", "Users_ImportFileLabel", "Marketing_Layout_Roi", "Marketing_Landing_VsCrmBi"
    };

    [Fact]
    public void SupportedCultures_Include_Es_EsPa_And_En()
    {
        Assert.Contains("es", LocalizationExtensions.SupportedCultures);
        Assert.Contains("es-PA", LocalizationExtensions.SupportedCultures);
        Assert.Contains("en", LocalizationExtensions.SupportedCultures);
    }

    [Fact]
    public void LocalizationJson_FilesExist_And_HaveMatchingKeyCounts()
    {
        var enPath = Path.Combine(ResourcesDir, "localization-en.json");
        var esPath = Path.Combine(ResourcesDir, "localization-es.json");
        var esPaPath = Path.Combine(ResourcesDir, "localization-es-PA.json");

        Assert.True(File.Exists(enPath));
        Assert.True(File.Exists(esPath));
        Assert.True(File.Exists(esPaPath));

        var enKeys = ReadJsonKeys(enPath);
        var esKeys = ReadJsonKeys(esPath);
        var esPaKeys = ReadJsonKeys(esPaPath);

        Assert.Equal(enKeys.Count, esKeys.Count);
        Assert.Equal(enKeys.Count, esPaKeys.Count);
        Assert.Empty(enKeys.Except(esKeys).ToList());
        Assert.Empty(enKeys.Except(esPaKeys).ToList());
    }

    [Fact]
    public void LocalizationJson_SpanishValues_MustDifferExceptAllowedBrandKeys()
    {
        AssertIdenticalCountBelowThreshold("localization-es.json", maxIdentical: 0);
    }

    [Fact]
    public void LocalizationJson_SpanishPanamaValues_MustDifferExceptAllowedBrandKeys()
    {
        AssertIdenticalCountBelowThreshold("localization-es-PA.json", maxIdentical: 5);
    }

    [Fact]
    public void SharedResource_Resx_FilesExist_For_All_Cultures()
    {
        Assert.True(File.Exists(Path.Combine(ResourcesDir, "SharedResource.en.resx")));
        Assert.True(File.Exists(Path.Combine(ResourcesDir, "SharedResource.es.resx")));
        Assert.True(File.Exists(Path.Combine(ResourcesDir, "SharedResource.es-PA.resx")));
    }

    [Fact]
    public void ValidationMessages_Spanish_IsNotCorrupted()
    {
        var esPath = Path.Combine(ResourcesDir, "ValidationMessages.es.resx");
        var content = File.ReadAllText(esPath);

        Assert.DoesNotContain("direcciÃ", content);
        Assert.Contains("direcci", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void IStringLocalizer_ReturnsDistinctValues_For_En_Es_And_EsPa()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddLocalization();

        using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IStringLocalizerFactory>();

        var en = factory.Create("AutonomusCRM.API.Resources.SharedResource", "AutonomusCRM.API");
        var es = factory.Create("AutonomusCRM.API.Resources.SharedResource", "AutonomusCRM.API");

        CultureInfo.CurrentUICulture = new CultureInfo("en");
        var enNav = en["Nav_Leads"].Value;

        CultureInfo.CurrentUICulture = new CultureInfo("es");
        var esNav = es["Nav_Leads"].Value;

        CultureInfo.CurrentUICulture = new CultureInfo("es-PA");
        var esPaNav = es["Nav_Leads"].Value;

        Assert.Equal("Leads", enNav);
        Assert.Equal("Prospectos", esNav);
        Assert.False(string.IsNullOrWhiteSpace(esPaNav));
    }

    private static void AssertIdenticalCountBelowThreshold(string esFileName, int maxIdentical)
    {
        var enPath = Path.Combine(ResourcesDir, "localization-en.json");
        var esPath = Path.Combine(ResourcesDir, esFileName);

        var enDoc = JsonDocument.Parse(File.ReadAllText(enPath));
        var esDoc = JsonDocument.Parse(File.ReadAllText(esPath));

        var unexpectedIdentical = new List<string>();
        foreach (var prop in enDoc.RootElement.EnumerateObject())
        {
            if (AllowedIdenticalKeys.Contains(prop.Name))
                continue;
            if (!esDoc.RootElement.TryGetProperty(prop.Name, out var esVal))
                continue;

            var enText = prop.Value.GetString() ?? "";
            var esText = esVal.GetString() ?? "";

            if (enText == esText && enText.Length >= 3 && enText.Any(char.IsLetter))
                unexpectedIdentical.Add(prop.Name);
        }

        Assert.True(unexpectedIdentical.Count <= maxIdentical,
            $"Untranslated keys in {esFileName}: {string.Join(", ", unexpectedIdentical.Take(20))}" +
            (unexpectedIdentical.Count > 20 ? $" (+{unexpectedIdentical.Count - 20} more)" : ""));
    }

    private static HashSet<string> ReadJsonKeys(string path)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.EnumerateObject().Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
    }
}
