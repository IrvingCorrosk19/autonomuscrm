using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Tenants;
using AutonomusCRM.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Tests.Integration;

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
public sealed class TenantIsolationIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public TenantIsolationIntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    private void RequirePostgres()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        Assert.NotNull(_fixture.Db);
    }

    [SkippableFact]
    public async Task EfQueryFilter_TenantA_CannotSee_TenantB_Customers()
    {
        RequirePostgres();
        var tenantAccessor = new TestTenantAccessor();
        var conn = _fixture.ConnectionString!;
        var ctx = new AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext(
            new DbContextOptionsBuilder<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>()
                .UseNpgsql(conn)
                .Options,
            tenantAccessor);

        var tenantA = Tenant.Create("Iso-A-" + Guid.NewGuid().ToString("N")[..8], "test");
        var tenantB = Tenant.Create("Iso-B-" + Guid.NewGuid().ToString("N")[..8], "test");
        await ctx.Tenants.AddRangeAsync(tenantA, tenantB);
        var custA = Customer.Create(tenantA.Id, "Customer A");
        var custB = Customer.Create(tenantB.Id, "Customer B");
        await ctx.Customers.AddRangeAsync(custA, custB);
        await ctx.SaveChangesAsync();

        tenantAccessor.BypassTenantFilter = false;
        tenantAccessor.TenantId = tenantA.Id;
        var visibleForA = await ctx.Customers.AsNoTracking().ToListAsync();
        Assert.Contains(visibleForA, c => c.Id == custA.Id);
        Assert.DoesNotContain(visibleForA, c => c.Id == custB.Id);

        tenantAccessor.TenantId = tenantB.Id;
        var visibleForB = await ctx.Customers.AsNoTracking().ToListAsync();
        Assert.Contains(visibleForB, c => c.Id == custB.Id);
        Assert.DoesNotContain(visibleForB, c => c.Id == custA.Id);

        await ctx.DisposeAsync();
    }

    [SkippableFact]
    public async Task EfQueryFilter_CrossTenant_GetById_ReturnsNull()
    {
        RequirePostgres();
        var tenantAccessor = new TestTenantAccessor();
        var conn = _fixture.ConnectionString!;
        await using var ctx = new AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext(
            new DbContextOptionsBuilder<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>()
                .UseNpgsql(conn)
                .Options,
            tenantAccessor);

        var tenantA = Tenant.Create("Iso-A2-" + Guid.NewGuid().ToString("N")[..8], "test");
        var tenantB = Tenant.Create("Iso-B2-" + Guid.NewGuid().ToString("N")[..8], "test");
        await ctx.Tenants.AddRangeAsync(tenantA, tenantB);
        var custB = Customer.Create(tenantB.Id, "Secret B");
        await ctx.Customers.AddAsync(custB);
        await ctx.SaveChangesAsync();

        tenantAccessor.BypassTenantFilter = false;
        tenantAccessor.TenantId = tenantA.Id;
        var leaked = await ctx.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.Id == custB.Id);
        Assert.Null(leaked);
    }

    [SkippableFact]
    public async Task Users_AreScoped_PerTenant()
    {
        RequirePostgres();
        var tenantAccessor = new TestTenantAccessor { BypassTenantFilter = true };
        var conn = _fixture.ConnectionString!;
        await using var ctx = new AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext(
            new DbContextOptionsBuilder<AutonomusCRM.Infrastructure.Persistence.ApplicationDbContext>()
                .UseNpgsql(conn)
                .Options,
            tenantAccessor);

        var tenantA = Tenant.Create("Iso-U-A-" + Guid.NewGuid().ToString("N")[..8], "test");
        var tenantB = Tenant.Create("Iso-U-B-" + Guid.NewGuid().ToString("N")[..8], "test");
        await ctx.Tenants.AddRangeAsync(tenantA, tenantB);
        var email = $"iso-{Guid.NewGuid():N}@test.local";
        var hash = BCrypt.Net.BCrypt.HashPassword("Test123!");
        var userA = User.Create(tenantA.Id, email, hash);
        userA.AddRole("Admin");
        var userB = User.Create(tenantB.Id, email, hash);
        userB.AddRole("Admin");
        await ctx.Users.AddRangeAsync(userA, userB);
        await ctx.SaveChangesAsync();

        tenantAccessor.BypassTenantFilter = false;
        tenantAccessor.TenantId = tenantA.Id;
        var usersA = await ctx.Users.AsNoTracking().Where(u => u.Email == email).ToListAsync();
        Assert.Single(usersA);
        Assert.Equal(tenantA.Id, usersA[0].TenantId);

        tenantAccessor.TenantId = tenantB.Id;
        var usersB = await ctx.Users.AsNoTracking().Where(u => u.Email == email).ToListAsync();
        Assert.Single(usersB);
        Assert.Equal(tenantB.Id, usersB[0].TenantId);
    }
}
