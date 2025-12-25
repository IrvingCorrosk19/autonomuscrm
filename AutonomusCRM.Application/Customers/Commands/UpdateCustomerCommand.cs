using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Customers.Commands;

public record UpdateCustomerCommand(
    Guid CustomerId,
    Guid TenantId,
    string Name,
    string? Email = null,
    string? Phone = null,
    string? Company = null,
    CustomerStatus? Status = null
) : IRequest<bool>;

