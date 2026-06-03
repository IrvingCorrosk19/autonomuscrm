namespace AutonomusCRM.Application.SaaS;

public class SaasPlanOptions
{
    public const string SectionName = "SaaS";

    public bool EnforceSubscription { get; set; }
    public int DefaultTrialDays { get; set; } = 14;
    public int MaxUsersFree { get; set; } = 5;
    public int MaxCustomersFree { get; set; } = 500;
}
