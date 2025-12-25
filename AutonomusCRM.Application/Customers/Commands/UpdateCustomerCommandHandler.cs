using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Customers.Commands;

public class UpdateCustomerCommandHandler : IRequestHandler<UpdateCustomerCommand, bool>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateCustomerCommandHandler> _logger;

    public UpdateCustomerCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateCustomerCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateCustomerCommand request, CancellationToken cancellationToken = default)
    {
        var customer = await _customerRepository.GetByIdAsync(request.CustomerId, cancellationToken);
        
        if (customer == null || customer.TenantId != request.TenantId)
        {
            _logger.LogWarning("Customer {CustomerId} not found or tenant mismatch", request.CustomerId);
            throw new InvalidOperationException("Cliente no encontrado o no pertenece al tenant");
        }

        // Actualizar información básica
        if (customer.Name != request.Name || customer.Email != request.Email || 
            customer.Phone != request.Phone || customer.Company != request.Company)
        {
            customer.UpdateInfo(request.Name, request.Email, request.Phone, request.Company);
        }

        // Actualizar estado si se proporciona
        if (request.Status.HasValue && customer.Status != request.Status.Value)
        {
            customer.ChangeStatus(request.Status.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Customer {CustomerId} updated successfully", request.CustomerId);
        
        return true;
    }
}

