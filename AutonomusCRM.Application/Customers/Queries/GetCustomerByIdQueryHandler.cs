using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Customers.Queries;

public class GetCustomerByIdQueryHandler : IRequestHandler<GetCustomerByIdQuery, CustomerDto?>
{
    private readonly ICustomerRepository _customerRepository;

    public GetCustomerByIdQueryHandler(ICustomerRepository customerRepository)
    {
        _customerRepository = customerRepository;
    }

    public async Task<CustomerDto?> HandleAsync(GetCustomerByIdQuery request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        
        if (customer == null || customer.TenantId != request.TenantId)
            return null;

        return new CustomerDto(
            customer.Id,
            customer.TenantId,
            customer.Name,
            customer.Email,
            customer.Phone,
            customer.Company,
            customer.Status,
            customer.LifetimeValue,
            customer.RiskScore,
            customer.CreatedAt
        );
    }
}

