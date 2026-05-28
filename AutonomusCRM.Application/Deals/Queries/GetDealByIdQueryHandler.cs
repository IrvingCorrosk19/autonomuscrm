using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Deals.Queries;

public class GetDealByIdQueryHandler : IRequestHandler<GetDealByIdQuery, DealDto?>
{
    private readonly IDealRepository _dealRepository;

    public GetDealByIdQueryHandler(IDealRepository dealRepository)
    {
        _dealRepository = dealRepository;
    }

    public async Task<DealDto?> HandleAsync(GetDealByIdQuery request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(request.DealId, cancellationToken);
        if (deal == null || deal.TenantId != request.TenantId)
            return null;

        return new DealDto(
            deal.Id,
            deal.TenantId,
            deal.CustomerId,
            deal.Title,
            deal.Amount,
            deal.Status,
            deal.Stage,
            deal.Probability,
            deal.ExpectedCloseDate,
            deal.CreatedAt,
            deal.Version);
    }
}
