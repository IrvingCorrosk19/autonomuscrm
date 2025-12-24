using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Customers.Commands;

public class UpdateCustomerStatusCommandHandler : IRequestHandler<UpdateCustomerStatusCommand, bool>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public UpdateCustomerStatusCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<bool> HandleAsync(UpdateCustomerStatusCommand request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        
        if (customer == null || customer.TenantId != request.TenantId)
            return false;

        customer.ChangeStatus(request.Status);
        await _customerRepository.UpdateAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(customer.DomainEvents, cancellationToken);
        customer.ClearDomainEvents();

        return true;
    }
}

