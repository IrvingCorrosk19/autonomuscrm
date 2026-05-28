namespace AutonomusCRM.Application.Autonomous;

public static class AutonomousConstants
{
    public const string DecisionRescue = "Rescue";
    public const string DecisionRenewal = "Renewal";
    public const string DecisionExpansion = "Expansion";
    public const string DecisionEscalation = "Escalation";
    public const string DecisionReEngagement = "ReEngagement";
    public const string DecisionUpsell = "Upsell";
    public const string DecisionNoAction = "NoAction";

    public const string PlaybookStateActive = "Active";
    public const string PlaybookStateCompleted = "Completed";
    public const string PlaybookStateEscalated = "Escalated";

    public const string AuditPending = "Pending";
    public const string AuditExecuted = "Executed";
    public const string AuditFailed = "Failed";

    public const string KnowledgeWin = "Win";
    public const string KnowledgeLoss = "Loss";
    public const string KnowledgeNeutral = "Neutral";

    public const string TaskAutonomous = "Autonomous_Action";
}
