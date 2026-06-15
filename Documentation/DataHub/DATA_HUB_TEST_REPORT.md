# Data Hub — Test Report

## Unit Tests (`AutonomusCRM.Tests/DataHub/DataHubUnitTests.cs`)

| Test | Result |
|------|--------|
| ParseCsvLine_HandlesQuotedCommas | Pass |
| DetectDelimiter_PrefersSemicolonForEuropeanCsv | Pass |
| NormalizeEmail_LowercasesAndTrims | Pass |
| Trim_RemovesWhitespace | Pass |
| ValidateUpload_RejectsPathTraversal | Pass |
| ValidateUpload_AcceptsValidCsv | Pass |
| SanitizeCellValue_PreventsCsvInjection | Pass |
| AutoMap_MatchesEmailSynonyms | Pass |
| GetFields_CustomerHasRequiredName | Pass |
| MaxFileBytes_AllowsLargeEnterpriseImports | Pass |
| AllowedExtensions_IncludesExcel | Pass |

## Manual QA Checklist

- [ ] Upload CSV leads → map → validate → import
- [ ] Upload XLSX customers with upsert
- [ ] Cancel job mid-import
- [ ] Download error CSV
- [ ] Export customers JSON
- [ ] Quality center shows duplicate emails
- [ ] Tenant A cannot access Tenant B job ID

## Build

```
dotnet build — SUCCESS
dotnet ef migrations add DataHubEnterpriseEtl — SUCCESS
dotnet test --filter DataHub — 11/11 PASS
```
