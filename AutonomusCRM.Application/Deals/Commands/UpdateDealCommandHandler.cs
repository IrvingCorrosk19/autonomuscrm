using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Deals.Commands;

public class UpdateDealCommandHandler : IRequestHandler<UpdateDealCommand, bool>
{
    private readonly IDealRepository _dealRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateDealCommandHandler> _logger;

    public UpdateDealCommandHandler(
        IDealRepository dealRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateDealCommandHandler> logger)
    {
        _dealRepository = dealRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateDealCommand request, CancellationToken cancellationToken = default)
    {
        var deal = await _dealRepository.GetByIdAsync(request.DealId, cancellationToken);
        
        if (deal == null || deal.TenantId != request.TenantId)
        {
            _logger.LogWarning("Deal {DealId} not found or tenant mismatch", request.DealId);
            throw new InvalidOperationException("Deal no encontrado o no pertenece al tenant");
        }

        // Actualizar información básica
        if (deal.Title != request.Title || deal.Description != request.Description || 
            (request.Amount.HasValue && deal.Amount != request.Amount.Value) ||
            (request.CustomerId.HasValue && deal.CustomerId != request.CustomerId.Value))
        {
            deal.UpdateInfo(request.Title, request.Description, request.Amount, request.CustomerId);
        }

        // Actualizar etapa si se proporciona
        if (request.Stage.HasValue && deal.Stage != request.Stage.Value)
        {
            deal.UpdateStage(request.Stage.Value, request.Probability);
        }
        else if (request.Probability.HasValue && deal.Probability != request.Probability.Value)
        {
            // Solo actualizar probabilidad sin cambiar etapa
            deal.UpdateProbability(request.Probability.Value);
        }

        // Actualizar fecha de cierre esperada
        if (request.ExpectedCloseDate.HasValue)
        {
            deal.SetExpectedCloseDate(request.ExpectedCloseDate.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Deal {DealId} updated successfully", request.DealId);
        
        return true;
    }
}

