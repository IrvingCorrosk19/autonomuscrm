using AutonomusCRM.Application.EnterpriseAuth;

namespace AutonomusCRM.Tests.EnterpriseAuth;

public class ScimUserRequestTests
{
    [Fact]
    public void ScimUserRequest_StoresRoles()
    {
        var req = new ScimUserRequest("u@corp.com", true, "Ana", "López", new[] { "Manager" });
        Assert.Single(req.Roles!);
        Assert.Equal("Manager", req.Roles![0]);
    }
}
