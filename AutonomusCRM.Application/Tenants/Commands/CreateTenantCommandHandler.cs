using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Tenants;

namespace AutonomusCRM.Application.Tenants.Commands;

public class CreateTenantCommandHandler : IRequestHandler<CreateTenantCommand, Guid>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Guid> HandleAsync(CreateTenantCommand request, CancellationToken cancellationToken = default)
    {
        var tenant = Tenant.Create(request.Name, request.Description);
        
        await _tenantRepository.AddAsync(tenant, cancellationToken);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        await _eventDispatcher.DispatchAsync(tenant.DomainEvents, cancellationToken);
        
        tenant.ClearDomainEvents();
        
        return tenant.Id;
    }
}

