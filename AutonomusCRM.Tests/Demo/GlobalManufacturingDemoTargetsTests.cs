using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Infrastructure.Persistence.Seed;

namespace AutonomusCRM.Tests.Demo;

[Trait("Category", "Demo")]
public class GlobalManufacturingDemoTargetsTests
{
    [Fact]
    public void TenantId_IsStable()
    {
        Assert.Equal(Guid.Parse("d0e00000-0000-4000-8000-000000000002"), TenantIds.GlobalManufacturing);
    }

    [Fact]
    public void Targets_MatchSprint2Brief()
    {
        Assert.Equal("Global Manufacturing Group", GlobalManufacturingDemoTargets.TenantName);
        Assert.Equal(50_000, GlobalManufacturingDemoTargets.Customers);
        Assert.Equal(500_000, GlobalManufacturingDemoTargets.ErpInvoices);
        Assert.Equal(2_000_000, GlobalManufacturingDemoTargets.ErpPayments);
        Assert.Equal(320, GlobalManufacturingDemoTargets.ErpProducts);
    }

    [Fact]
    public void LiteMode_IsSmallerThanFullDemo()
    {
        Assert.True(GlobalManufacturingDemoTargets.LiteCustomers < GlobalManufacturingDemoTargets.Customers);
        Assert.True(GlobalManufacturingDemoTargets.LiteDeals < GlobalManufacturingDemoTargets.Deals);
    }
}
