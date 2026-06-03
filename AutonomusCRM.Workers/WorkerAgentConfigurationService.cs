using System.Text.Json;
using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Workers;

/// <summary>Default agent config for worker host (autonomous enabled without UI query stack).</summary>
public sealed class WorkerAgentConfigurationService : IAgentConfigurationService
{
    private static readonly Dictionary<string, object> DefaultConfig = new()
    {
        ["IsEnabled"] = true,
        ["AutonomousMode"] = true
    };

    public Task<Dictionary<string, object>> GetConfigAsync(Guid tenantId, string agentName, CancellationToken cancellationToken = default)
        => Task.FromResult(new Dictionary<string, object>(DefaultConfig));

    public bool IsEnabled(Dictionary<string, object> config)
    {
        if (!config.TryGetValue("IsEnabled", out var value))
            return true;
        return value switch
        {
            bool b => b,
            JsonElement je when je.ValueKind == JsonValueKind.True => true,
            JsonElement je when je.ValueKind == JsonValueKind.False => false,
            _ => true
        };
    }

    public T GetValue<T>(Dictionary<string, object> config, string key, T defaultValue)
    {
        if (!config.TryGetValue(key, out var raw) || raw is null)
            return defaultValue;
        try
        {
            if (raw is T direct) return direct;
            return (T)Convert.ChangeType(raw, typeof(T))!;
        }
        catch
        {
            return defaultValue;
        }
    }
}
