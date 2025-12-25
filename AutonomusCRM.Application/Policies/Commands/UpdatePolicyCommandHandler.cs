using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Policies.Commands;

public class UpdatePolicyCommandHandler : IRequestHandler<UpdatePolicyCommand, bool>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdatePolicyCommandHandler> _logger;

    public UpdatePolicyCommandHandler(
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdatePolicyCommandHandler> logger)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdatePolicyCommand request, CancellationToken cancellationToken = default)
    {
        var policy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken);
        
        if (policy == null || policy.TenantId != request.TenantId)
        {
            _logger.LogWarning("Policy {PolicyId} not found or tenant mismatch", request.PolicyId);
            throw new InvalidOperationException("Política no encontrada o no pertenece al tenant");
        }

        if (policy.Name != request.Name || policy.Expression != request.Expression || policy.Description != request.Description)
        {
            policy.UpdateInfo(request.Name, request.Expression, request.Description);
        }

        if (request.IsActive.HasValue && policy.IsActive != request.IsActive.Value)
        {
            policy.SetActive(request.IsActive.Value);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);
        _logger.LogInformation("Policy {PolicyId} updated successfully", request.PolicyId);
        
        return true;
    }
}

