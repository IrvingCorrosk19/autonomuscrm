namespace AutonomusCRM.Tests.DataPlatform;

public class IdentityResolutionLogicTests
{
    [Fact]
    public void EmailGrouping_FindsDuplicates()
    {
        var rows = new[]
        {
            (Id: Guid.NewGuid(), Email: "a@corp.com"),
            (Id: Guid.NewGuid(), Email: "A@corp.com"),
            (Id: Guid.NewGuid(), Email: "b@corp.com")
        };

        var groups = rows
            .GroupBy(r => r.Email.Trim().ToLowerInvariant())
            .Where(g => g.Count() > 1)
            .ToList();

        Assert.Single(groups);
        Assert.Equal("a@corp.com", groups[0].Key);
        Assert.Equal(2, groups[0].Count());
    }
}
