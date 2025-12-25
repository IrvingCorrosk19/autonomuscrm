using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Customers.Commands;

public record DeleteCustomerCommand(
    Guid CustomerId,
    Guid TenantId
) : IRequest<bool>;

