using System.Text.Json;
using AutonomusCRM.Application.Agents.Queries;
using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Infrastructure.Agents;

public class AgentConfigurationService : IAgentConfigurationService
{
    private readonly IRequestHandler<GetAgentConfigQuery, Dictionary<string, object>> _getConfigHandler;

    public AgentConfigurationService(IRequestHandler<GetAgentConfigQuery, Dictionary<string, object>> getConfigHandler)
    {
        _getConfigHandler = getConfigHandler;
    }

    public Task<Dictionary<string, object>> GetConfigAsync(Guid tenantId, string agentName, CancellationToken cancellationToken = default)
        => _getConfigHandler.HandleAsync(new GetAgentConfigQuery(tenantId, agentName), cancellationToken);

    public bool IsEnabled(Dictionary<string, object> config)
    {
        if (!config.TryGetValue("IsEnabled", out var value))
            return true;

        return value switch
        {
            bool b => b,
            JsonElement je when je.ValueKind == JsonValueKind.True => true,
            JsonElement je when je.ValueKind == JsonValueKind.False => false,
            string s => bool.TryParse(s, out var parsed) && parsed,
            _ => true
        };
    }

    public T GetValue<T>(Dictionary<string, object> config, string key, T defaultValue)
    {
        if (!config.TryGetValue(key, out var raw) || raw is null)
            return defaultValue;

        try
        {
            if (raw is T direct)
                return direct;

            if (raw is JsonElement je)
            {
                return je.ValueKind switch
                {
                    JsonValueKind.Number when typeof(T) == typeof(int) => (T)(object)je.GetInt32(),
                    JsonValueKind.Number when typeof(T) == typeof(double) => (T)(object)je.GetDouble(),
                    JsonValueKind.True when typeof(T) == typeof(bool) => (T)(object)true,
                    JsonValueKind.False when typeof(T) == typeof(bool) => (T)(object)false,
                    JsonValueKind.String => (T)Convert.ChangeType(je.GetString(), typeof(T))!,
                    _ => defaultValue
                };
            }

            return (T)Convert.ChangeType(raw, typeof(T))!;
        }
        catch
        {
            return defaultValue;
        }
    }
}
