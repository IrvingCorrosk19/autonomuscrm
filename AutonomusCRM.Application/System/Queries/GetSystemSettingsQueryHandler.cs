using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Application.System.Queries;

public class GetSystemSettingsQueryHandler : IRequestHandler<GetSystemSettingsQuery, Dictionary<string, object>>
{
    private readonly ILogger<GetSystemSettingsQueryHandler> _logger;
    private readonly IConfiguration _configuration;

    public GetSystemSettingsQueryHandler(
        ILogger<GetSystemSettingsQueryHandler> logger,
        IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task<Dictionary<string, object>> HandleAsync(GetSystemSettingsQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            var settings = new Dictionary<string, object>
            {
                ["Region"] = _configuration["Region:Current"] ?? "us-east-1",
                ["TimeZone"] = "America/Panama",
                ["MfaRequired"] = true,
                ["KillSwitch"] = false,
                ["MinConfidence"] = 0.75,
                ["OperationMode"] = "Supervised",
                ["ActiveAgents"] = 7,
                ["TotalAgents"] = 7
            };

            return await Task.FromResult(settings);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting system settings");
            return new Dictionary<string, object>();
        }
    }
}

