using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AutonomusCRM.Application.Agents.Commands;

public class UpdateAgentConfigCommandHandler : IRequestHandler<UpdateAgentConfigCommand, bool>
{
    private readonly ILogger<UpdateAgentConfigCommandHandler> _logger;
    private readonly ITenantRepository _tenantRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAgentConfigCommandHandler(
        ILogger<UpdateAgentConfigCommandHandler> logger,
        ITenantRepository tenantRepository,
        IUnitOfWork unitOfWork)
    {
        _logger = logger;
        _tenantRepository = tenantRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<bool> HandleAsync(UpdateAgentConfigCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found", request.TenantId);
                return false;
            }

            // Guardar configuración del agente en los settings del tenant
            var configKey = $"AgentConfig_{request.AgentName}";
            var configJson = JsonSerializer.Serialize(request.Configuration);
            tenant.UpdateSetting(configKey, configJson);

            await _tenantRepository.UpdateAsync(tenant, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Agent configuration updated: {AgentName} for Tenant {TenantId}", 
                request.AgentName, request.TenantId);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating agent configuration");
            return false;
        }
    }
}

