namespace AutonomusCRM.Application.Autonomous;

/// <summary>
/// Global kill-switch for autonomous AI execution (maps to AI__Enabled / Autonomous:Enabled).
/// </summary>
public class AutonomousPlatformOptions
{
    public const string SectionName = "Autonomous";

    /// <summary>When false, autonomous cycles and decision execution are skipped.</summary>
    public bool Enabled { get; set; } = true;
}
