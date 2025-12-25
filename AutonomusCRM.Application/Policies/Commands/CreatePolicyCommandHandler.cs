using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Policies;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Policies.Commands;

public class CreatePolicyCommandHandler : IRequestHandler<CreatePolicyCommand, Guid>
{
    private readonly IPolicyRepository _policyRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<CreatePolicyCommandHandler> _logger;

    public CreatePolicyCommandHandler(
        IPolicyRepository policyRepository,
        IUnitOfWork unitOfWork,
        ILogger<CreatePolicyCommandHandler> logger)
    {
        _policyRepository = policyRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<Guid> HandleAsync(CreatePolicyCommand request, CancellationToken cancellationToken = default)
    {
        var policy = Policy.Create(request.TenantId, request.Name, request.Expression, request.Description);
        
        await _policyRepository.AddAsync(policy, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        
        _logger.LogInformation("Policy {PolicyId} created successfully", policy.Id);
        return policy.Id;
    }
}

