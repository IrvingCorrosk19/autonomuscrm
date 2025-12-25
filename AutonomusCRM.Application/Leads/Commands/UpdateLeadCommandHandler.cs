using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Leads.Events;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Leads.Commands;

public class UpdateLeadCommandHandler : IRequestHandler<UpdateLeadCommand, bool>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateLeadCommandHandler> _logger;

    public UpdateLeadCommandHandler(
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateLeadCommandHandler> logger)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateLeadCommand request, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(request.LeadId, cancellationToken);
        
        if (lead == null || lead.TenantId != request.TenantId)
        {
            _logger.LogWarning("Lead {LeadId} not found or tenant mismatch", request.LeadId);
            throw new InvalidOperationException("Lead no encontrado o no pertenece al tenant");
        }

        // Actualizar información básica
        if (lead.Name != request.Name || lead.Source != request.Source || 
            lead.Email != request.Email || lead.Phone != request.Phone || lead.Company != request.Company)
        {
            lead.UpdateInfo(request.Name, request.Email, request.Phone, request.Company, request.Source);
        }

        // Actualizar estado si se proporciona
        if (request.Status.HasValue && lead.Status != request.Status.Value)
        {
            lead.ChangeStatus(request.Status.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Lead {LeadId} updated successfully", request.LeadId);
        
        return true;
    }
}

