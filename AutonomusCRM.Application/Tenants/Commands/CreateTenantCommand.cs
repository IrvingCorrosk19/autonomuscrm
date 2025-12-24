using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tenants.Commands;

public record CreateTenantCommand(string Name, string? Description) : IRequest<Guid>;

