using AutonomusCRM.Application.Billing;

namespace AutonomusCRM.Tests.Billing;

public class TenantBillingAccountTests
{
    [Theory]
    [InlineData(BillingPlans.Free, 5, 500)]
    [InlineData(BillingPlans.Professional, 50, 20000)]
    [InlineData(BillingPlans.Enterprise, 500, 500000)]
    public void PlanLimits_AreApplied(string planId, int maxUsers, int maxCustomers)
    {
        var account = TenantBillingAccount.Create(Guid.NewGuid(), planId);
        account.ApplyStripe("cus_x", "sub_x", "active", DateTime.UtcNow.AddMonths(1), planId);
        Assert.Equal(maxUsers, account.MaxUsers);
        Assert.Equal(maxCustomers, account.MaxCustomers);
    }
}
