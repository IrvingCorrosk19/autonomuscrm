using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Customers.Commands;

public class DeleteCustomerCommandHandler : IRequestHandler<DeleteCustomerCommand, bool>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeleteCustomerCommandHandler> _logger;

    public DeleteCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeleteCustomerCommand request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        
        if (customer == null || customer.TenantId != request.TenantId)
        {
            _logger.LogWarning("Customer {CustomerId} not found or tenant mismatch", request.CustomerId);
            throw new InvalidOperationException("Cliente no encontrado o no pertenece al tenant");
        }

        await _customerRepository.DeleteAsync(customer, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Customer {CustomerId} deleted successfully", request.CustomerId);
        return true;
    }
}

