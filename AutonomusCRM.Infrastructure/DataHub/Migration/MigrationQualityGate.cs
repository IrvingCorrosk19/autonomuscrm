using AutonomusCRM.Application.DataHub;

namespace AutonomusCRM.Infrastructure.DataHub.Migration;

public static class MigrationQualityGate
{
    public static MigrationQualityEvaluation Evaluate(
        IReadOnlyList<DataHubImportError> errors,
        DataHubDuplicateScanResultDto dupes)
    {
        var issues = new List<string>();
        if (errors.Count > 0)
            issues.Add($"{errors.Count} validation/import errors");
        if (dupes.Groups.Count > 0)
            issues.Add($"{dupes.Groups.Count} duplicate groups detected");

        var brokenRelations = errors.Count(e =>
            e.ErrorCode.Contains("Relation", StringComparison.OrdinalIgnoreCase) ||
            e.ErrorCode.Contains("Foreign", StringComparison.OrdinalIgnoreCase) ||
            e.Message.Contains("not found", StringComparison.OrdinalIgnoreCase));

        var missingOwners = errors.Count(e =>
            e.FieldName?.Contains("Owner", StringComparison.OrdinalIgnoreCase) == true ||
            e.ErrorCode.Contains("Owner", StringComparison.OrdinalIgnoreCase));

        if (brokenRelations > 0) issues.Add($"{brokenRelations} broken relation errors");
        if (missingOwners > 0) issues.Add($"{missingOwners} missing owner references");

        var passed = errors.Count == 0 && dupes.Groups.Count == 0 && brokenRelations == 0 && missingOwners == 0;
        return new MigrationQualityEvaluation(dupes.Groups.Count, errors.Count, brokenRelations, missingOwners, issues, passed);
    }
}

public sealed record MigrationQualityEvaluation(
    int DuplicateGroups,
    int ErrorCount,
    int BrokenRelations,
    int MissingOwners,
    IReadOnlyList<string> Issues,
    bool Passed);
