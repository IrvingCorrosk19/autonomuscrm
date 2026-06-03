namespace AutonomusCRM.Application.EnterpriseAuth;

public class ScimGroup
{
    public Guid Id { get; private set; }
    public Guid TenantId { get; private set; }
    public string DisplayName { get; private set; } = string.Empty;
    public List<string> MemberEmails { get; private set; } = new();
    public DateTime CreatedAt { get; private set; }

    private ScimGroup() { }

    public static ScimGroup Create(Guid tenantId, string displayName, IEnumerable<string> members)
        => new()
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            DisplayName = displayName,
            MemberEmails = members.Select(m => m.Trim().ToLowerInvariant()).Distinct().ToList(),
            CreatedAt = DateTime.UtcNow
        };

    public void AddMember(string email)
    {
        var normalized = email.Trim().ToLowerInvariant();
        if (!MemberEmails.Contains(normalized))
            MemberEmails.Add(normalized);
    }
}
