using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Tenants;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.Tenants.Commands;

public class UpdateTenantCommandHandler : IRequestHandler<UpdateTenantCommand, bool>
{
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UpdateTenantCommandHandler> _logger;

    public UpdateTenantCommandHandler(
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork,
        ILogger<UpdateTenantCommandHandler> logger)
    {
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> HandleAsync(UpdateTenantCommand request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        
        if (tenant == null)
        {
            _logger.LogWarning("Tenant {TenantId} not found", request.TenantId);
            return false;
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            tenant.UpdateName(request.Name);
        }

        if (!string.IsNullOrWhiteSpace(request.Email))
        {
            tenant.UpdateSetting("Email", request.Email);
        }
        
        if (!string.IsNullOrWhiteSpace(request.Region))
        {
            tenant.UpdateSetting("Region", request.Region);
        }
        
        if (!string.IsNullOrWhiteSpace(request.TimeZone))
        {
            tenant.UpdateSetting("TimeZone", request.TimeZone);
        }

        await _tenantRepository.UpdateAsync(tenant, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Tenant {TenantId} updated successfully", request.TenantId);
        return true;
    }
}

