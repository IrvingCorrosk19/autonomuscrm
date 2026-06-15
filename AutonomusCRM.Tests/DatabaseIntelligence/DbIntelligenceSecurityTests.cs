using System.Security.Claims;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence;
using Microsoft.AspNetCore.Http;
using Moq;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceSecurityTests
{
    [Fact]
    public void ConnectionProfileDto_DoesNotExposePasswordField()
    {
        var dtoType = typeof(DbConnectionProfileDto);
        Assert.Null(dtoType.GetProperty("Password"));
        Assert.Null(dtoType.GetProperty("Username"));
    }

    [Fact]
    public void MaskUsername_HidesMiddleCharacters()
    {
        Assert.Equal("a******r", DbIntelligenceMasking.MaskUsername("administrator"));
        Assert.Equal("**", DbIntelligenceMasking.MaskUsername("ab"));
    }

    [Fact]
    public void TenantGuard_FailClosed_WhenTenantMissing()
    {
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(new DefaultHttpContext());
        var guard = new DbIntelligenceTenantGuard(accessor.Object);
        Assert.False(guard.IsSameTenant(Guid.NewGuid()));
        Assert.Throws<DbIntelligenceTenantAccessException>(() => guard.EnsureSameTenant(Guid.NewGuid()));
    }

    [Fact]
    public void TenantGuard_RejectsCrossTenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("TenantId", tenantA.ToString())
            ], "test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(context);
        var guard = new DbIntelligenceTenantGuard(accessor.Object);

        Assert.True(guard.IsSameTenant(tenantA));
        Assert.False(guard.IsSameTenant(tenantB));
    }

    [Fact]
    public void SanitizeErrorMessage_RedactsPasswordFragments()
    {
        var sanitized = DbConnectionStringValidator.SanitizeErrorMessage("Login failed for user x; Password=Secret123;");
        Assert.DoesNotContain("Secret123", sanitized);
        Assert.Contains("Password=***", sanitized);
    }

    [Fact]
    public void ValidateHost_RejectsSemicolonInjection()
    {
        var ex = Assert.Throws<DbIntelligenceValidationException>(() =>
            DbConnectionStringValidator.ValidateHost("localhost;drop table users"));
        Assert.Contains("invalid", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
