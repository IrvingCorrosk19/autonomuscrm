using AutonomusCRM.Application.EnterpriseAuth;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.EnterpriseAuth;

public sealed class ScimGroupService : IScimGroupService
{
    private readonly ApplicationDbContext _db;

    public ScimGroupService(ApplicationDbContext db) => _db = db;

    public async Task<ScimGroupResponse> CreateGroupAsync(
        Guid tenantId, ScimGroupRequest request, CancellationToken cancellationToken = default)
    {
        var group = ScimGroup.Create(tenantId, request.DisplayName, request.Members);
        _db.ScimGroups.Add(group);
        await _db.SaveChangesAsync(cancellationToken);
        return Map(group);
    }

    public async Task<ScimGroupResponse?> GetGroupAsync(
        Guid tenantId, Guid groupId, CancellationToken cancellationToken = default)
    {
        var g = await _db.ScimGroups.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == groupId, cancellationToken);
        return g == null ? null : Map(g);
    }

    public async Task<IReadOnlyList<ScimGroupResponse>> ListGroupsAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var list = await _db.ScimGroups.Where(g => g.TenantId == tenantId).ToListAsync(cancellationToken);
        return list.Select(Map).ToList();
    }

    public async Task AddMemberAsync(
        Guid tenantId, Guid groupId, string userEmail, CancellationToken cancellationToken = default)
    {
        var g = await _db.ScimGroups.FirstOrDefaultAsync(x => x.TenantId == tenantId && x.Id == groupId, cancellationToken)
            ?? throw new InvalidOperationException("Group not found");
        g.AddMember(userEmail);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ScimGroupResponse Map(ScimGroup g)
        => new(g.Id, g.DisplayName, g.MemberEmails);
}
