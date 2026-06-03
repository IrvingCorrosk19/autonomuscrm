using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.EnterpriseAI;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/ai")]
[Authorize]
public class AiController : ControllerBase
{
    [HttpGet("dashboard")]
    public async Task<ActionResult<ExecutiveAiDashboardDto>> GetDashboard(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        var svc = HttpContext.RequestServices.GetRequiredService<IExecutiveAiDashboardService>();
        return Ok(await svc.GetDashboardAsync(tenantId, cancellationToken));
    }

    [HttpGet("decisions")]
    public async Task<ActionResult<IReadOnlyList<AutonomousDecisionDto>>> GetDecisions(
        [FromQuery] Guid tenantId, [FromQuery] int take = 50, CancellationToken cancellationToken = default)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IAiDecisionAuditService>()
            .GetRecentAsync(tenantId, take, cancellationToken));
    }

    [HttpGet("next-best-actions")]
    public async Task<ActionResult<IReadOnlyList<NextBestActionDto>>> GetNba(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<INextBestActionEngine>()
            .GetForTenantAsync(tenantId, cancellationToken));
    }

    [HttpGet("predictions")]
    public async Task<ActionResult<PredictiveRevenueForecastDto>> GetPredictions(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IPredictiveRevenueEngine>()
            .ForecastAsync(tenantId, cancellationToken));
    }

    [HttpGet("knowledge")]
    public async Task<ActionResult<IReadOnlyList<KnowledgeInsightDto>>> GetKnowledge(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IBusinessKnowledgeEngine>()
            .GetInsightsAsync(tenantId, cancellationToken));
    }

    [HttpGet("ml-datasets")]
    public async Task<ActionResult<IReadOnlyList<MlDatasetSummaryDto>>> GetMlDatasets(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IMlFoundationService>()
            .GetDatasetSummaryAsync(tenantId, cancellationToken));
    }

    [HttpPost("decide/{customerId:guid}")]
    public async Task<ActionResult<AutonomousDecisionDto>> DecideCustomer(
        [FromQuery] Guid tenantId, Guid customerId, [FromQuery] bool execute = true, CancellationToken cancellationToken = default)
    {
        var engine = HttpContext.RequestServices.GetRequiredService<IAutonomousRevenueDecisionEngine>();
        var decision = await engine.DecideForCustomerAsync(tenantId, customerId, cancellationToken);
        if (execute && decision.DecisionType != AutonomousConstants.DecisionNoAction)
            await engine.ExecuteDecisionAsync(tenantId, decision, cancellationToken);
        return Ok(decision);
    }

    [HttpPost("cycle")]
    public async Task<ActionResult> RunAutonomousCycle([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        await HttpContext.RequestServices.GetRequiredService<IAutonomousOrchestrationEngine>()
            .RunAutonomousCycleAsync(tenantId, cancellationToken);
        return Ok(new { status = "completed" });
    }

    [HttpPost("audits/{auditId:guid}/execution-outcome")]
    public async Task<ActionResult> MarkExecutionOutcome(
        Guid auditId, [FromQuery] string outcome, [FromQuery] bool success = true, CancellationToken cancellationToken = default)
    {
        await HttpContext.RequestServices.GetRequiredService<IAiDecisionAuditService>()
            .MarkExecutionOutcomeAsync(auditId, outcome, success, cancellationToken);
        return Ok(new { auditId, outcomeType = "execution", success });
    }

    [HttpPost("audits/{auditId:guid}/business-outcome")]
    public async Task<ActionResult> MarkBusinessOutcome(
        Guid auditId, [FromQuery] bool succeeded, [FromQuery] string detail, CancellationToken cancellationToken = default)
    {
        await HttpContext.RequestServices.GetRequiredService<IAiDecisionAuditService>()
            .MarkBusinessOutcomeAsync(auditId, succeeded, detail, cancellationToken);
        return Ok(new { auditId, outcomeType = "business", succeeded, detail });
    }

    [HttpGet("analytics")]
    public async Task<ActionResult<ExecutiveAiAnalyticsDto>> GetAnalytics(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IExecutiveAiAnalyticsService>()
            .GetAnalyticsAsync(tenantId, cancellationToken));
    }

    [HttpGet("governance")]
    public async Task<ActionResult<AiGovernanceReportDto>> GetGovernance(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IAiGovernanceService>()
            .GetGovernanceReportAsync(tenantId, cancellationToken));
    }

    [HttpGet("models")]
    public async Task<ActionResult<IReadOnlyList<MlModelVersionDto>>> ListModels(
        [FromQuery] Guid tenantId, [FromQuery] string modelType, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IModelRegistryService>()
            .ListVersionsAsync(tenantId, modelType, cancellationToken));
    }

    [HttpPost("models/train")]
    public async Task<ActionResult<MlTrainResultDto>> TrainModel(
        [FromQuery] Guid tenantId, [FromQuery] string modelType, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IMachineLearningPipelineService>()
            .TrainModelAsync(tenantId, modelType, cancellationToken));
    }

    [HttpPost("models/train-all")]
    public async Task<ActionResult<IReadOnlyList<MlTrainResultDto>>> TrainAll(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IMachineLearningPipelineService>()
            .TrainAllAsync(tenantId, cancellationToken));
    }

    [HttpPost("models/rollback")]
    public async Task<ActionResult> RollbackModel(
        [FromQuery] Guid tenantId, [FromQuery] string modelType, [FromQuery] string versionTag, CancellationToken cancellationToken)
    {
        var ok = await HttpContext.RequestServices.GetRequiredService<IModelRegistryService>()
            .RollbackAsync(tenantId, modelType, versionTag, cancellationToken);
        return ok ? Ok(new { status = "rolled_back" }) : NotFound();
    }

    [HttpGet("ml/churn")]
    public async Task<ActionResult<IReadOnlyList<ChurnMlPredictionDto>>> GetChurnMl(
        [FromQuery] Guid tenantId, [FromQuery] Guid? customerId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IChurnPredictionModel>()
            .PredictAsync(tenantId, customerId, cancellationToken));
    }

    [HttpGet("ml/expansion")]
    public async Task<ActionResult<IReadOnlyList<ExpansionMlPredictionDto>>> GetExpansionMl(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IExpansionPredictionModel>()
            .PredictAsync(tenantId, cancellationToken: cancellationToken));
    }

    [HttpGet("ml/revenue")]
    public async Task<ActionResult<RevenueMlForecastDto>> GetRevenueMl(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IRevenuePredictionModel>()
            .ForecastAsync(tenantId, cancellationToken));
    }

    [HttpGet("evaluation")]
    public async Task<ActionResult<IReadOnlyList<AiEvaluationMetricsDto>>> GetEvaluation(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IAiEvaluationFrameworkService>()
            .EvaluateAllAsync(tenantId, cancellationToken));
    }

    [HttpGet("knowledge-graph")]
    public async Task<ActionResult<KnowledgeGraphDto>> GetKnowledgeGraph(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        return Ok(await HttpContext.RequestServices.GetRequiredService<IBusinessKnowledgeGraphService>()
            .GetGraphAsync(tenantId, cancellationToken: cancellationToken));
    }

    [HttpPost("enterprise-cycle")]
    public async Task<ActionResult> RunEnterpriseAiCycle([FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        await HttpContext.RequestServices.GetRequiredService<IEnterpriseAiCycleService>()
            .RunEnterpriseAiCycleAsync(tenantId, cancellationToken);
        return Ok(new { status = "enterprise_ai_completed" });
    }
}
