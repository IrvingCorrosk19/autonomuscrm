using AutonomusCRM.Application.EnterpriseAuth;

namespace AutonomusCRM.Tests.EnterpriseAuth;

public class ScimGroupTests
{
    [Fact]
    public void CreateGroup_NormalizesMembers()
    {
        var g = ScimGroup.Create(Guid.NewGuid(), "Sales", new[] { "A@X.com", "a@x.com", "B@X.com" });
        Assert.Equal(2, g.MemberEmails.Count);
        Assert.Contains("a@x.com", g.MemberEmails);
    }

    [Fact]
    public void AddMember_IsIdempotent()
    {
        var g = ScimGroup.Create(Guid.NewGuid(), "CS", Array.Empty<string>());
        g.AddMember("user@test.com");
        g.AddMember("user@test.com");
        Assert.Single(g.MemberEmails);
    }
}
