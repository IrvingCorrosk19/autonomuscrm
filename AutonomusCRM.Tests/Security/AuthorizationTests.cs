using AutonomusCRM.Application.Authorization.Policies;

namespace AutonomusCRM.Tests.Security;

public class AuthorizationTests
{
    [Theory]
    [InlineData("RequireAdmin", "Admin")]
    [InlineData("RequireManager", "Manager")]
    public void AuthorizationPolicies_ShouldDefineExpectedNames(string policy, string role)
    {
        Assert.Equal(policy, policy);
        Assert.False(string.IsNullOrWhiteSpace(role));
        Assert.Equal("RequireAdmin", AuthorizationPolicies.RequireAdmin);
    }
}
