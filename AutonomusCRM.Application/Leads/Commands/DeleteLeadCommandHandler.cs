using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Leads.Commands;

public class DeleteLeadCommandHandler : IRequestHandler<DeleteLeadCommand, bool>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteLeadCommandHandler> _logger;

    public DeleteLeadCommandHandler(
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteLeadCommandHandler> logger)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeleteLeadCommand request, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(request.LeadId, cancellationToken);
        
        if (lead == null || lead.TenantId != request.TenantId)
        {
            _logger.LogWarning("Lead {LeadId} not found or tenant mismatch", request.LeadId);
            throw new InvalidOperationException("Lead no encontrado o no pertenece al tenant");
        }

        await _leadRepository.DeleteAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Lead {LeadId} deleted successfully", request.LeadId);
        return true;
    }
}

