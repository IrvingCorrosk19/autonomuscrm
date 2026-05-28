using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Deals.Commands;

public class LoseDealCommandHandler : IRequestHandler<LoseDealCommand, bool>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public LoseDealCommandHandler(
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> HandleAsync(LoseDealCommand request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(request.DealId, cancellationToken);
        if (deal == null || deal.TenantId != request.TenantId)
            return false;

        deal.Lose(request.Reason, request.LossCategory);
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(deal.DomainEvents, cancellationToken);
        deal.ClearDomainEvents();
        return true;
    }
}
