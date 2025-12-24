using AutonomusCRM.Domain.Tenants;
using Xunit;

namespace AutonomusCRM.Tests.Domain;

public class TenantTests
{
    [Fact]
    public void Create_ShouldCreateTenantWithValidData()
    {
        // Arrange
        var name = "Test Tenant";
        var description = "Test Description";

        // Act
        var tenant = Tenant.Create(name, description);

        // Assert
        Assert.NotNull(tenant);
        Assert.Equal(name, tenant.Name);
        Assert.Equal(description, tenant.Description);
        Assert.True(tenant.IsActive);
        Assert.False(tenant.IsKillSwitchEnabled);
        Assert.NotEqual(Guid.Empty, tenant.Id);
    }

    [Fact]
    public void Activate_ShouldSetIsActiveToTrue()
    {
        // Arrange
        var tenant = Tenant.Create("Test Tenant");
        tenant.Deactivate();

        // Act
        tenant.Activate();

        // Assert
        Assert.True(tenant.IsActive);
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        // Arrange
        var tenant = Tenant.Create("Test Tenant");

        // Act
        tenant.Deactivate();

        // Assert
        Assert.False(tenant.IsActive);
    }

    [Fact]
    public void EnableKillSwitch_ShouldSetIsKillSwitchEnabledToTrue()
    {
        // Arrange
        var tenant = Tenant.Create("Test Tenant");

        // Act
        tenant.EnableKillSwitch();

        // Assert
        Assert.True(tenant.IsKillSwitchEnabled);
    }

    [Fact]
    public void DisableKillSwitch_ShouldSetIsKillSwitchEnabledToFalse()
    {
        // Arrange
        var tenant = Tenant.Create("Test Tenant");
        tenant.EnableKillSwitch();

        // Act
        tenant.DisableKillSwitch();

        // Assert
        Assert.False(tenant.IsKillSwitchEnabled);
    }
}

