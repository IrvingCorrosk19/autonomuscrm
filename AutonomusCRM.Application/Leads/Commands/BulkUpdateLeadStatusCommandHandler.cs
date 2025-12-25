using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Leads.Commands;

public class BulkUpdateLeadStatusCommandHandler : IRequestHandler<BulkUpdateLeadStatusCommand, int>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkUpdateLeadStatusCommandHandler> _logger;

    public BulkUpdateLeadStatusCommandHandler(
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        ILogger<BulkUpdateLeadStatusCommandHandler> logger)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> HandleAsync(BulkUpdateLeadStatusCommand request, CancellationToken cancellationToken = default)
    {
        var updatedCount = 0;
        
        foreach (var leadId in request.LeadIds)
        {
            try
            {
                var lead = await _leadRepository.GetByIdAsync(leadId, cancellationToken);
                
                if (lead != null && lead.TenantId == request.TenantId && lead.Status != request.NewStatus)
                {
                    lead.ChangeStatus(request.NewStatus);
                    updatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating lead {LeadId} in bulk operation", leadId);
            }
        }
        
        if (updatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        _logger.LogInformation("Bulk updated {Count} leads to status {Status}", updatedCount, request.NewStatus);
        return updatedCount;
    }
}

