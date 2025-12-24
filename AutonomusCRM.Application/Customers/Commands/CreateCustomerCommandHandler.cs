using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Application.Customers.Commands;

public class CreateCustomerCommandHandler : IRequestHandler<CreateCustomerCommand, Guid>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IDomainEventDispatcher _eventDispatcher;

    public CreateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        IDomainEventDispatcher eventDispatcher)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _eventDispatcher = eventDispatcher;
    }

    public async Task<Guid> HandleAsync(CreateCustomerCommand request, CancellationToken cancellationToken = default)
    {
        var customer = Customer.Create(
            request.TenantId,
            request.Name,
            request.Email,
            request.Phone,
            request.Company);

        await _customerRepository.AddAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        await _eventDispatcher.DispatchAsync(customer.DomainEvents, cancellationToken);
        customer.ClearDomainEvents();

        return customer.Id;
    }
}

