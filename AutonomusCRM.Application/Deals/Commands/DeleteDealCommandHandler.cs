using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Deals.Commands;

public class DeleteDealCommandHandler : IRequestHandler<DeleteDealCommand, bool>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteDealCommandHandler> _logger;

    public DeleteDealCommandHandler(
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteDealCommandHandler> logger)
    {
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeleteDealCommand request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(request.DealId, cancellationToken);
        
        if (deal == null || deal.TenantId != request.TenantId)
        {
            _logger.LogWarning("Deal {DealId} not found or tenant mismatch", request.DealId);
            throw new InvalidOperationException("Deal no encontrado o no pertenece al tenant");
        }

        await _dealRepository.DeleteAsync(deal, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Deal {DealId} deleted successfully", request.DealId);
        return true;
    }
}

