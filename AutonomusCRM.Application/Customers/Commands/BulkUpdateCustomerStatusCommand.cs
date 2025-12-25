using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Customers.Commands;

public record BulkUpdateCustomerStatusCommand(
    List<Guid> CustomerIds,
    Guid TenantId,
    CustomerStatus NewStatus
) : IRequest<int>;

