using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Customers.Commands;

public record CreateCustomerCommand(
    Guid TenantId,
    string Name,
    string? Email = null,
    string? Phone = null,
    string? Company = null
) : IRequest<Guid>;

