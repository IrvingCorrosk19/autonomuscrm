using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Customers.Commands;

public record UpdateCustomerStatusCommand(
    Guid CustomerId,
    Guid TenantId,
    CustomerStatus Status
) : IRequest<bool>;

