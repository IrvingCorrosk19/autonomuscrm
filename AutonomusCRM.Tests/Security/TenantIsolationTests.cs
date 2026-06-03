using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Tenants;

namespace AutonomusCRM.Tests.Security;

public class TenantIsolationTests
{
    [Fact]
    public void Customer_BelongsToTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var c1 = Customer.Create(tenantA, "A");
        var c2 = Customer.Create(tenantB, "B");
        Assert.NotEqual(c1.TenantId, c2.TenantId);
    }

    [Fact]
    public void Tenant_Create_HasUniqueId()
    {
        var t1 = Tenant.Create("T1", "d");
        var t2 = Tenant.Create("T2", "d");
        Assert.NotEqual(t1.Id, t2.Id);
    }
}
