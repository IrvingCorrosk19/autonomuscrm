using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface ICustomerRepository : IRepository<Customer>
{
    Task<IEnumerable<Customer>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Customer?> GetByEmailAsync(Guid tenantId, string email, CancellationToken cancellationToken = default);
    Task<IEnumerable<Customer>> GetByStatusAsync(Guid tenantId, CustomerStatus status, CancellationToken cancellationToken = default);
}

