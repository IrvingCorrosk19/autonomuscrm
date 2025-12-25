using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace AutonomusCRM.Application.Agents.Queries;

public class GetAgentConfigQueryHandler : IRequestHandler<GetAgentConfigQuery, Dictionary<string, object>>
{
    private readonly ILogger<GetAgentConfigQueryHandler> _logger;
    private readonly ITenantRepository _tenantRepository;

    public GetAgentConfigQueryHandler(
        ILogger<GetAgentConfigQueryHandler> logger,
        ITenantRepository tenantRepository)
    {
        _logger = logger;
        _tenantRepository = tenantRepository;
    }

    public async Task<Dictionary<string, object>> HandleAsync(GetAgentConfigQuery request, CancellationToken cancellationToken = default)
    {
        try
        {
            var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
            if (tenant == null)
            {
                _logger.LogWarning("Tenant {TenantId} not found", request.TenantId);
                return GetDefaultConfig(request.AgentName);
            }

            var configKey = $"AgentConfig_{request.AgentName}";
            if (tenant.Settings.TryGetValue(configKey, out var configJson))
            {
                var config = JsonSerializer.Deserialize<Dictionary<string, object>>(configJson);
                if (config != null)
                    return config;
            }

            return GetDefaultConfig(request.AgentName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting agent configuration");
            return GetDefaultConfig(request.AgentName);
        }
    }

    private Dictionary<string, object> GetDefaultConfig(string agentName)
    {
        return agentName switch
        {
            "LeadIntelligenceAgent" => new Dictionary<string, object>
            {
                ["IsEnabled"] = true,
                ["MinConfidence"] = 0.7,
                ["SourceWeights"] = new Dictionary<string, int>
                {
                    ["Referral"] = 30,
                    ["Website"] = 20,
                    ["SocialMedia"] = 15,
                    ["EmailCampaign"] = 10,
                    ["Other"] = 5
                },
                ["ContactWeights"] = new Dictionary<string, int>
                {
                    ["Email"] = 15,
                    ["Phone"] = 10,
                    ["Company"] = 20
                }
            },
            "CustomerRiskAgent" => new Dictionary<string, object>
            {
                ["IsEnabled"] = true,
                ["MinConfidence"] = 0.75,
                ["BaseRiskScore"] = 50,
                ["RiskAdjustments"] = new Dictionary<string, int>
                {
                    ["NoEmail"] = 10,
                    ["NoPhone"] = 5,
                    ["HasCompany"] = -15
                },
                ["HighRiskThreshold"] = 70,
                ["LowRiskThreshold"] = 30
            },
            "DealStrategyAgent" => new Dictionary<string, object>
            {
                ["IsEnabled"] = true,
                ["MinConfidence"] = 0.8,
                ["HighValueLTVThreshold"] = 10000,
                ["HighValueLTVBonus"] = 10,
                ["LowRiskBonus"] = 5,
                ["HighRiskPenalty"] = -10,
                ["StagnantDaysThreshold"] = 30,
                ["StagnantPenalty"] = -5,
                ["RiskDaysThreshold"] = 20,
                ["RiskProbabilityThreshold"] = 30
            },
            "CommunicationAgent" => new Dictionary<string, object>
            {
                ["IsEnabled"] = true,
                ["MinConfidence"] = 0.7,
                ["BestContactHours"] = new[] { 9, 10, 11, 14, 15, 16 },
                ["AutoWelcomeEmail"] = true,
                ["AutoFollowUp"] = true,
                ["Channels"] = new[] { "Email", "SMS" }
            },
            "DataQualityGuardian" => new Dictionary<string, object>
            {
                ["IsEnabled"] = true,
                ["MinConfidence"] = 0.9,
                ["ScanIntervalMinutes"] = 60,
                ["AutoFixEnabled"] = false,
                ["RequiredFields"] = new Dictionary<string, string[]>
                {
                    ["Customer"] = new[] { "Email" },
                    ["Lead"] = new[] { "Email", "Phone" }
                }
            },
            "ComplianceSecurityAgent" => new Dictionary<string, object>
            {
                ["IsEnabled"] = true,
                ["MinConfidence"] = 0.95,
                ["RequireCorrelationId"] = true,
                ["RequireTenantId"] = true,
                ["BlockOnKillSwitch"] = true,
                ["SensitiveEventTypes"] = new[] { "User.", "Tenant.KillSwitch", "Customer.RiskScore" }
            },
            "AutomationOptimizerAgent" => new Dictionary<string, object>
            {
                ["IsEnabled"] = true,
                ["MinConfidence"] = 0.85,
                ["AnalysisIntervalMinutes"] = 120,
                ["OptimizationThreshold"] = 0.1,
                ["AutoApplyOptimizations"] = false
            },
            _ => new Dictionary<string, object> { ["IsEnabled"] = true, ["MinConfidence"] = 0.75 }
        };
    }
}

