using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Commands;

public class CloseDealCommandHandler : IRequestHandler<CloseDealCommand, bool>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CloseDealCommandHandler(
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> HandleAsync(CloseDealCommand request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(request.DealId, cancellationToken);
        
        if (deal == null || deal.TenantId != request.TenantId)
            return false;

        deal.Close(DateTime.UtcNow, request.FinalAmount);
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(deal.DomainEvents, cancellationToken);
        deal.ClearDomainEvents();

        return true;
    }
}

