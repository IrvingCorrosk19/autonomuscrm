using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Policies.Commands;

public class DeletePolicyCommandHandler : IRequestHandler<DeletePolicyCommand, bool>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DeletePolicyCommandHandler> _logger;

    public DeletePolicyCommandHandler(
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork,
        ILogger<DeletePolicyCommandHandler> logger)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(DeletePolicyCommand request, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken);
        
        if (policy == null || policy.TenantId != request.TenantId)
        {
            _logger.LogWarning("Policy {PolicyId} not found or tenant mismatch", request.PolicyId);
            throw new InvalidOperationException("Política no encontrada o no pertenece al tenant");
        }

        await _policyRepository.DeleteAsync(policy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Policy {PolicyId} deleted successfully", request.PolicyId);
        return true;
    }
}

