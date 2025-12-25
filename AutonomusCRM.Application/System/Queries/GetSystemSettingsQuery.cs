using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.System.Queries;

public record GetSystemSettingsQuery(
    Guid TenantId
) : IRequest<Dictionary<string, object>>;

