using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.KnowledgeGraph;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.Infrastructure.KnowledgeGraph;

public sealed class OperationalGraphFeedService : IOperationalGraphFeed
{
    private readonly IKnowledgeGraphRepository _graph;
    private readonly AutonomusCRM.Application.Common.Interfaces.IUnitOfWork _uow;
    private readonly ILogger<OperationalGraphFeedService> _logger;

    public OperationalGraphFeedService(
        IKnowledgeGraphRepository graph,
        AutonomusCRM.Application.Common.Interfaces.IUnitOfWork uow,
        ILogger<OperationalGraphFeedService> logger)
    {
        _graph = graph;
        _uow = uow;
        _logger = logger;
    }

    public async Task RecordTrustApprovalQueuedAsync(Guid tenantId, Guid approvalId, Guid auditId, string decisionType, CancellationToken cancellationToken = default)
    {
        try
        {
            await LinkAsync(tenantId, KnowledgeGraphNodeTypes.Decision, auditId, KnowledgeGraphNodeTypes.Approval, approvalId, KnowledgeGraphRelations.RequiredApproval, 1m, cancellationToken);
            await LinkAsync(tenantId, KnowledgeGraphNodeTypes.TrustDecision, auditId, KnowledgeGraphNodeTypes.Decision, auditId, KnowledgeGraphRelations.ContextFor, 1m, cancellationToken);
            var riskId = DeterministicId($"trust-risk:{auditId}");
            await LinkAsync(tenantId, KnowledgeGraphNodeTypes.TrustRisk, riskId, KnowledgeGraphNodeTypes.Decision, auditId, KnowledgeGraphRelations.TrustPolicyApplies, 0.8m, cancellationToken);
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Trust graph feed skipped for approval {ApprovalId}", approvalId);
        }
    }

    public Task RecordTrustApprovedAsync(Guid tenantId, Guid approvalId, Guid auditId, CancellationToken cancellationToken = default)
        => SafeLinkAsync(tenantId,
            KnowledgeGraphNodeTypes.Approval, approvalId,
            KnowledgeGraphNodeTypes.Decision, auditId,
            KnowledgeGraphRelations.EnabledExecution, 1m, null, cancellationToken);

    public Task RecordTrustRejectedAsync(Guid tenantId, Guid approvalId, Guid auditId, CancellationToken cancellationToken = default)
        => SafeLinkAsync(tenantId,
            KnowledgeGraphNodeTypes.Rejection, approvalId,
            KnowledgeGraphNodeTypes.Decision, auditId,
            KnowledgeGraphRelations.BlockedExecution, 1m, null, cancellationToken);

    public Task RecordTrustRollbackAsync(Guid tenantId, Guid approvalId, Guid auditId, CancellationToken cancellationToken = default)
        => SafeLinkAsync(tenantId,
            KnowledgeGraphNodeTypes.Rollback, approvalId,
            KnowledgeGraphNodeTypes.Outcome, DeterministicId($"audit-outcome:{auditId}"),
            KnowledgeGraphRelations.ReversedOutcome, 1m, null, cancellationToken);

    public async Task RecordCommunicationAsync(
        Guid tenantId, Guid communicationLogId, string channel, Guid? customerId, string? agentName,
        CancellationToken cancellationToken = default)
    {
        var commType = MapChannel(channel);
        await SafeLinkAsync(tenantId,
            KnowledgeGraphNodeTypes.Communication, communicationLogId,
            customerId.HasValue ? KnowledgeGraphNodeTypes.Customer : KnowledgeGraphNodeTypes.Communication,
            customerId ?? communicationLogId,
            KnowledgeGraphRelations.SentCommunication, 1m,
            async () =>
            {
                if (customerId.HasValue)
                    await LinkAsync(tenantId, KnowledgeGraphNodeTypes.Customer, customerId.Value, KnowledgeGraphNodeTypes.Communication, communicationLogId, KnowledgeGraphRelations.ReceivedCommunication, 1m, cancellationToken);

                if (!string.IsNullOrWhiteSpace(agentName))
                {
                    var agentId = DeterministicId($"agent:{tenantId}:{agentName}");
                    await LinkAsync(tenantId, KnowledgeGraphNodeTypes.Agent, agentId, KnowledgeGraphNodeTypes.Communication, communicationLogId, KnowledgeGraphRelations.SentCommunication, 1m, cancellationToken);
                }

                await LinkAsync(tenantId, KnowledgeGraphNodeTypes.Communication, communicationLogId, KnowledgeGraphNodeTypes.Memory, communicationLogId, KnowledgeGraphRelations.LinkedToMemory, 0.7m, cancellationToken);
            },
            cancellationToken);
    }

    public async Task RecordVoiceCallAsync(
        Guid tenantId, Guid callId, Guid? customerId, string outcome, string? summary,
        CancellationToken cancellationToken = default)
    {
        await SafeLinkAsync(tenantId,
            KnowledgeGraphNodeTypes.VoiceCall, callId,
            customerId.HasValue ? KnowledgeGraphNodeTypes.Customer : KnowledgeGraphNodeTypes.VoiceCall,
            customerId ?? callId,
            KnowledgeGraphRelations.ContextFor, 1m,
            async () =>
            {
                if (customerId.HasValue)
                    await LinkAsync(tenantId, KnowledgeGraphNodeTypes.Customer, customerId.Value, KnowledgeGraphNodeTypes.VoiceCall, callId, KnowledgeGraphRelations.ReceivedCommunication, 1m, cancellationToken);

                var transcriptId = DeterministicId($"transcript:{callId}");
                await LinkAsync(tenantId, KnowledgeGraphNodeTypes.VoiceCall, callId, KnowledgeGraphNodeTypes.Transcript, transcriptId, KnowledgeGraphRelations.GeneratedMemory, 1m, cancellationToken);
                await LinkAsync(tenantId, KnowledgeGraphNodeTypes.Transcript, transcriptId, KnowledgeGraphNodeTypes.Memory, callId, KnowledgeGraphRelations.LinkedToMemory, 0.8m, cancellationToken);

                if (outcome.Contains("churn", StringComparison.OrdinalIgnoreCase) || outcome.Contains("risk", StringComparison.OrdinalIgnoreCase))
                {
                    var riskId = DeterministicId($"voice-risk:{callId}");
                    await LinkAsync(tenantId, KnowledgeGraphNodeTypes.VoiceCall, callId, KnowledgeGraphNodeTypes.TrustRisk, riskId, KnowledgeGraphRelations.CreatedRiskSignal, 1m, cancellationToken);
                }
                else if (outcome.Contains("expan", StringComparison.OrdinalIgnoreCase) || outcome.Contains("upsell", StringComparison.OrdinalIgnoreCase))
                {
                    var expId = DeterministicId($"voice-exp:{callId}");
                    await LinkAsync(tenantId, KnowledgeGraphNodeTypes.VoiceCall, callId, KnowledgeGraphNodeTypes.Revenue, expId, KnowledgeGraphRelations.CreatedExpansionSignal, 1m, cancellationToken);
                }

                if (!string.IsNullOrWhiteSpace(summary))
                {
                    var decisionProxy = DeterministicId($"voice-decision:{callId}");
                    await LinkAsync(tenantId, KnowledgeGraphNodeTypes.VoiceCall, callId, KnowledgeGraphNodeTypes.Decision, decisionProxy, KnowledgeGraphRelations.InfluencedDecision, 0.7m, cancellationToken);
                }
            },
            cancellationToken);
    }

    private async Task SafeLinkAsync(
        Guid tenantId, string fromType, Guid fromId, string toType, Guid toId, string relation, decimal weight,
        Func<Task>? extra, CancellationToken cancellationToken)
    {
        try
        {
            await LinkAsync(tenantId, fromType, fromId, toType, toId, relation, weight, cancellationToken);
            if (extra != null) await extra();
            await _uow.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Operational graph feed skipped {Relation}", relation);
        }
    }

    private async Task LinkAsync(Guid tenantId, string fromType, Guid fromId, string toType, Guid toId, string relation, decimal weight, CancellationToken cancellationToken)
    {
        await _graph.AddEdgeAsync(BusinessKnowledgeGraphEdge.Link(tenantId, fromType, fromId, toType, toId, relation, weight), cancellationToken);
    }

    private static string MapChannel(string channel) => channel.ToLowerInvariant() switch
    {
        "email" => KnowledgeGraphNodeTypes.Communication,
        "whatsapp" => KnowledgeGraphNodeTypes.Communication,
        _ => KnowledgeGraphNodeTypes.Communication
    };

    private static Guid DeterministicId(string key)
    {
        var hash = System.Security.Cryptography.MD5.HashData(System.Text.Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }
}
