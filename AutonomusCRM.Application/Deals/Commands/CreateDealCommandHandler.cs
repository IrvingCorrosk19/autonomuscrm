using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Commands;

public class CreateDealCommandHandler : IRequestHandler<CreateDealCommand, Guid>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateDealCommandHandler(
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Guid> HandleAsync(CreateDealCommand request, CancellationToken cancellationToken = default)
    {
        var deal = Deal.Create(
            request.TenantId,
            request.CustomerId,
            request.Title,
            request.Amount,
            request.Description);

        await _dealRepository.AddAsync(deal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(deal.DomainEvents, cancellationToken);
        deal.ClearDomainEvents();

        return deal.Id;
    }
}

