using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Leads.Commands;

public class QualifyLeadCommandHandler : IRequestHandler<QualifyLeadCommand, bool>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public QualifyLeadCommandHandler(
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> HandleAsync(QualifyLeadCommand request, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(request.LeadId, cancellationToken);
        
        if (lead == null || lead.TenantId != request.TenantId)
            return false;

        lead.Qualify();
        await _leadRepository.UpdateAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(lead.DomainEvents, cancellationToken);
        lead.ClearDomainEvents();

        return true;
    }
}

