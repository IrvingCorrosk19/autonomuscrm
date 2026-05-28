using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Deals.Commands;

public class BulkUpdateDealStageCommandHandler : IRequestHandler<BulkUpdateDealStageCommand, int>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;
    private readonly ILogger<BulkUpdateDealStageCommandHandler> _logger;

    public BulkUpdateDealStageCommandHandler(
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher,
        ILogger<BulkUpdateDealStageCommandHandler> logger)
    {
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
        _logger = logger;
    }

    public async Task<int> HandleAsync(BulkUpdateDealStageCommand request, CancellationToken cancellationToken = default)
    {
        var updatedCount = 0;
        var pendingEvents = new List<AutonomusCRM.Domain.Events.IDomainEvent>();

        foreach (var dealId in request.DealIds)
        {
            try
            {
                var deal = await _dealRepository.GetByIdAsync(dealId, cancellationToken);

                if (deal != null && deal.TenantId == request.TenantId && deal.Stage != request.NewStage)
                {
                    deal.UpdateStage(request.NewStage, request.Probability);
                    pendingEvents.AddRange(deal.DomainEvents);
                    deal.ClearDomainEvents();
                    updatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating deal {DealId} in bulk operation", dealId);
            }
        }

        if (updatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            await _eventDispatcher.DispatchAsync(pendingEvents, cancellationToken);
        }

        _logger.LogInformation("Bulk updated {Count} deals to stage {Stage}", updatedCount, request.NewStage);
        return updatedCount;
    }
}

