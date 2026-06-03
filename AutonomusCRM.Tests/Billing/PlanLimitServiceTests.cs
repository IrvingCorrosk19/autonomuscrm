using AutonomusCRM.Application.Billing;

namespace AutonomusCRM.Tests.Billing;

public class PlanLimitServiceTests
{
    [Theory]
    [InlineData(BillingPlans.Free, 5, 500, 1000)]
    [InlineData(BillingPlans.Enterprise, 500, 500_000, 2_000_000)]
    public void PlanLimitsProfile_MatchesPlan(string plan, int users, int customers, int leads)
    {
        var p = PlanLimitsProfile.ForPlan(plan);
        Assert.Equal(users, p.MaxUsers);
        Assert.Equal(customers, p.MaxCustomers);
        Assert.Equal(leads, p.MaxLeads);
    }
}
