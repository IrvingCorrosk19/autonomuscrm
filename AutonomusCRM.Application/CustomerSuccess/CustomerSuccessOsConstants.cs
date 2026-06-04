namespace AutonomusCRM.Application.CustomerSuccess;

/// <summary>Task types for Customer Success OS (tickets &amp; cases on WorkflowTask).</summary>
public static class CustomerSuccessOsConstants
{
    public const string Ticket = "CS_Ticket";

    public const string CaseRenewal = "CS_Case_Renewal";
    public const string CaseRecovery = "CS_Case_Recovery";
    public const string CaseExpansion = "CS_Case_Expansion";
    public const string CaseAtRisk = "CS_Case_AtRisk";

    public const string PlaybookAtRisk = "AtRisk";

    public static readonly IReadOnlyList<string> CaseTypes =
        [CaseRenewal, CaseRecovery, CaseExpansion, CaseAtRisk];

    public static bool IsTicket(string? taskType) =>
        string.Equals(taskType, Ticket, StringComparison.Ordinal);

    public static bool IsCase(string? taskType) =>
        taskType?.StartsWith("CS_Case_", StringComparison.Ordinal) == true;

    public static string CaseLabel(string? taskType) => taskType switch
    {
        CaseRenewal => "Renovación",
        CaseRecovery => "Recuperación",
        CaseExpansion => "Expansión",
        CaseAtRisk => "Cliente en riesgo",
        _ => "Caso"
    };
}
