using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Policies.Commands;

public class DuplicatePolicyCommandHandler : IRequestHandler<DuplicatePolicyCommand, Guid>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<DuplicatePolicyCommandHandler> _logger;

    public DuplicatePolicyCommandHandler(
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork,
        ILogger<DuplicatePolicyCommandHandler> logger)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(DuplicatePolicyCommand request, CancellationToken cancellationToken = default)
    {
        var originalPolicy = await _policyRepository.GetByIdAsync(request.PolicyId, cancellationToken);
        
        if (originalPolicy == null || originalPolicy.TenantId != request.TenantId)
        {
            _logger.LogWarning("Policy {PolicyId} not found or tenant mismatch", request.PolicyId);
            throw new InvalidOperationException("Política no encontrada o no pertenece al tenant");
        }

        var newName = request.NewName ?? $"{originalPolicy.Name} (Copia)";
        var newPolicy = Policy.Create(request.TenantId, newName, originalPolicy.Expression, originalPolicy.Description);
        
        if (!originalPolicy.IsActive)
        {
            newPolicy.Deactivate();
        }
        
        await _policyRepository.AddAsync(newPolicy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Policy {PolicyId} duplicated to {NewPolicyId}", request.PolicyId, newPolicy.Id);
        return newPolicy.Id;
    }
}

