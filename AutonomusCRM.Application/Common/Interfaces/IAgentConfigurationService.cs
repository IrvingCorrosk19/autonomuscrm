namespace AutonomusCRM.Application.Common.Interfaces;

public interface IAgentConfigurationService
{
    Task<Dictionary<string, object>> GetConfigAsync(Guid tenantId, string agentName, CancellationToken cancellationToken = default);
    bool IsEnabled(Dictionary<string, object> config);
    T GetValue<T>(Dictionary<string, object> config, string key, T defaultValue);
}
