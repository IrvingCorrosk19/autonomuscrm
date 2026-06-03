namespace AutonomusCRM.Application.EnterpriseAuth;

public record ScimGroupRequest(string DisplayName, IReadOnlyList<string> Members);

public record ScimGroupResponse(Guid Id, string DisplayName, IReadOnlyList<string> Members);

public interface IScimGroupService
{
    Task<ScimGroupResponse> CreateGroupAsync(Guid tenantId, ScimGroupRequest request, CancellationToken cancellationToken = default);
    Task<ScimGroupResponse?> GetGroupAsync(Guid tenantId, Guid groupId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScimGroupResponse>> ListGroupsAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task AddMemberAsync(Guid tenantId, Guid groupId, string userEmail, CancellationToken cancellationToken = default);
}
