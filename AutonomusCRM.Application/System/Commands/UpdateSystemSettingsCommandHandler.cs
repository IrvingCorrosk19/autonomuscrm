using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AutonomusCRM.Application.System.Commands;

public class UpdateSystemSettingsCommandHandler : IRequestHandler<UpdateSystemSettingsCommand, bool>
{
    private readonly ILogger<UpdateSystemSettingsCommandHandler> _logger;
    private readonly IConfiguration _configuration;

    public UpdateSystemSettingsCommandHandler(
        ILogger<UpdateSystemSettingsCommandHandler> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<bool> HandleAsync(UpdateSystemSettingsCommand request, CancellationToken cancellationToken = default)
    {
        try
        {
            // En una implementación real, esto guardaría en base de datos o archivo de configuración
            // Por ahora, solo logueamos los cambios
            _logger.LogInformation("System settings updated for tenant {TenantId}: {Settings}", 
                request.TenantId, JsonSerializer.Serialize(request.Settings));
            
            return await Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating system settings");
            return false;
        }
    }
}

