using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Customers.Queries;

public record GetCustomerByIdQuery(Guid CustomerId, Guid TenantId) : IRequest<CustomerDto?>;

public record CustomerDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Email,
    string? Phone,
    string? Company,
    CustomerStatus Status,
    decimal? LifetimeValue,
    int? RiskScore,
    DateTime CreatedAt
);

