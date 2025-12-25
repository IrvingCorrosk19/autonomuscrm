using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Deals.Commands;

public class UpdateDealProbabilityCommandHandler : IRequestHandler<UpdateDealProbabilityCommand, bool>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<UpdateDealProbabilityCommandHandler> _logger;

    public UpdateDealProbabilityCommandHandler(
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<UpdateDealProbabilityCommandHandler> logger)
    {
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateDealProbabilityCommand request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(request.DealId, cancellationToken);
        
        if (deal == null || deal.TenantId != request.TenantId)
        {
            _logger.LogWarning("Deal {DealId} not found or tenant mismatch", request.DealId);
            return false;
        }

        deal.UpdateProbability(request.Probability);
        await _dealRepository.UpdateAsync(deal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(deal.DomainEvents, cancellationToken);
        deal.ClearDomainEvents();

        _logger.LogInformation("Deal {DealId} probability updated to {Probability}%", request.DealId, request.Probability);
        return true;
    }
}

