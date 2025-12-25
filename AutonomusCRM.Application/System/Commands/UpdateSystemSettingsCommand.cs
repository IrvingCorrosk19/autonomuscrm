using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.System.Commands;

public record UpdateSystemSettingsCommand(
    Guid TenantId,
    Dictionary<string, object> Settings
) : IRequest<bool>;

