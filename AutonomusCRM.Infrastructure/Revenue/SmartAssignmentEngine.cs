using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Infrastructure.Revenue;

public class SmartAssignmentEngine : ISmartAssignmentEngine
{
    private readonly IUserRepository _userRepository;
    private readonly ILeadRepository _leadRepository;
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;

    public SmartAssignmentEngine(
        IUserRepository userRepository,
        ILeadRepository leadRepository,
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _leadRepository = leadRepository;
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid?> GetRecommendedOwnerAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = (await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken)).Where(u => u.IsActive).ToList();
        if (!users.Any())
            return null;

        var leads = await _leadRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var deals = await _dealRepository.GetByTenantIdAsync(tenantId, cancellationToken);

        var load = users.Select(u => new
        {
            u.Id,
            Load = leads.Count(l => l.AssignedToUserId == u.Id && l.Status != LeadStatus.Converted && l.Status != LeadStatus.Lost)
                 + deals.Count(d => d.AssignedToUserId == u.Id && d.Status == DealStatus.Open)
        }).OrderBy(x => x.Load).First();

        return load.Id;
    }

    public async Task<Guid?> AssignLeadToBestRepAsync(Guid tenantId, Guid leadId, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
        if (lead == null || lead.TenantId != tenantId)
            return null;

        if (lead.AssignedToUserId.HasValue)
            return lead.AssignedToUserId;

        var ownerId = await GetRecommendedOwnerAsync(tenantId, cancellationToken);
        if (!ownerId.HasValue)
            return null;

        lead.AssignToUser(ownerId.Value);
        await _leadRepository.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return ownerId;
    }
}
