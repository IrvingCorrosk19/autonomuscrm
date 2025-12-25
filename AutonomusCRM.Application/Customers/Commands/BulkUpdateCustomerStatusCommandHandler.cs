using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Customers;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Customers.Commands;

public class BulkUpdateCustomerStatusCommandHandler : IRequestHandler<BulkUpdateCustomerStatusCommand, int>
{
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<BulkUpdateCustomerStatusCommandHandler> _logger;

    public BulkUpdateCustomerStatusCommandHandler(
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork,
        ILogger<BulkUpdateCustomerStatusCommandHandler> logger)
    {
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> HandleAsync(BulkUpdateCustomerStatusCommand request, CancellationToken cancellationToken = default)
    {
        var updatedCount = 0;
        
        foreach (var customerId in request.CustomerIds)
        {
            try
            {
                var customer = await _customerRepository.GetByIdAsync(customerId, cancellationToken);
                
                if (customer != null && customer.TenantId == request.TenantId && customer.Status != request.NewStatus)
                {
                    customer.ChangeStatus(request.NewStatus);
                    updatedCount++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error updating customer {CustomerId} in bulk operation", customerId);
            }
        }
        
        if (updatedCount > 0)
        {
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }
        
        _logger.LogInformation("Bulk updated {Count} customers to status {Status}", updatedCount, request.NewStatus);
        return updatedCount;
    }
}

