using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tenants.Commands;

public record UpdateTenantCommand(
    Guid TenantId,
    string? Name = null,
    string? Email = null,
    string? Region = null,
    string? TimeZone = null
) : IRequest<bool>;

