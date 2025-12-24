using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Leads.Commands;

public class CreateLeadCommandHandler : IRequestHandler<CreateLeadCommand, Guid>
{
    private readonly ILeadRepository _leadRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateLeadCommandHandler(
        ILeadRepository leadRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _leadRepository = leadRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Guid> HandleAsync(CreateLeadCommand request, CancellationToken cancellationToken = default)
    {
        var lead = Lead.Create(
            request.TenantId,
            request.Name,
            request.Source,
            request.Email,
            request.Phone,
            request.Company);

        await _leadRepository.AddAsync(lead, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(lead.DomainEvents, cancellationToken);
        lead.ClearDomainEvents();

        return lead.Id;
    }
}

