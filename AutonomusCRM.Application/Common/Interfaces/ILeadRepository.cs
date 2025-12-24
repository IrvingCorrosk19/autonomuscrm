using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Common.Interfaces;

public interface ILeadRepository : IRepository<Lead>
{
    Task<IEnumerable<Lead>> GetByTenantIdAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lead>> GetByStatusAsync(Guid tenantId, LeadStatus status, CancellationToken cancellationToken = default);
    Task<IEnumerable<Lead>> GetByAssignedUserAsync(Guid tenantId, Guid userId, CancellationToken cancellationToken = default);
}

