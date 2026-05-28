namespace AutonomusCRM.Application.Automation;

/// <summary>Identificadores para automatización operativa del sistema (no workflows de usuario).</summary>
public static class OperationalConstants
{
    public static readonly Guid SystemWorkflowId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public const string TaskTypeAtRisk = "AtRisk";
    public const string TaskTypeFollowUp = "FollowUp";
    public const string TaskTypeOnboarding = "Onboarding";
}
