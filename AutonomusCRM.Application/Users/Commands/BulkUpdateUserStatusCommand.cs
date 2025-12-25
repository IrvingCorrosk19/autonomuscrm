using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Users.Commands;

public record BulkUpdateUserStatusCommand(
    List<Guid> UserIds,
    Guid TenantId,
    bool IsActive
) : IRequest<int>;

