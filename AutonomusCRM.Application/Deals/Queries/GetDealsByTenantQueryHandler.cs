using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Queries;

public class GetDealsByTenantQueryHandler : IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>>
{
    private readonly IDealRepository _dealRepository;

    public GetDealsByTenantQueryHandler(IDealRepository dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task<IEnumerable<DealDto>> HandleAsync(GetDealsByTenantQuery request, CancellationToken cancellationToken = default)
    {
        IEnumerable<Deal> deals;

        if (request.Status.HasValue)
        {
            deals = await _dealRepository.GetByStatusAsync(request.TenantId, request.Status.Value, cancellationToken);
        }
        else if (request.Stage.HasValue)
        {
            deals = await _dealRepository.GetByStageAsync(request.TenantId, request.Stage.Value, cancellationToken);
        }
        else
        {
            deals = await _dealRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        }

        return deals.Select(deal => new DealDto(
            deal.Id,
            deal.TenantId,
            deal.CustomerId,
            deal.Title,
            deal.Amount,
            deal.Status,
            deal.Stage,
            deal.Probability,
            deal.ExpectedCloseDate,
            deal.CreatedAt
        ));
    }
}

