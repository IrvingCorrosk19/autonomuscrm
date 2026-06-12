using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Revenue;

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
        var users = await _userRepository.GetActiveUserSummariesAsync(tenantId, cancellationToken);
        if (users.Count == 0)
            return null;

        var leadLoads = await _leadRepository.GetActiveAssignmentLoadByUserAsync(tenantId, cancellationToken);
        var dealLoads = await _dealRepository.GetOpenAssignmentLoadByUserAsync(tenantId, cancellationToken);

        return users
            .Select(u => new
            {
                u.Id,
                Load = leadLoads.GetValueOrDefault(u.Id) + dealLoads.GetValueOrDefault(u.Id)
            })
            .OrderBy(x => x.Load)
            .ThenBy(x => x.Id)
            .First()
            .Id;
    }

    public async Task<Guid?> AssignLeadToBestRepAsync(
        Guid tenantId, Guid leadId, CancellationToken cancellationToken = default)
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
