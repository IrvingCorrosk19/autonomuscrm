using System.Security.Claims;
using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/db-intelligence")]
[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public sealed class DatabaseIntelligenceController : ControllerBase
{
    private readonly IDbConnectionProfileService _connections;
    private readonly IDbSchemaDiscoveryService _discovery;
    private readonly IBusinessDiscoveryService _businessDiscovery;
    private readonly IDataHealthService _health;
    private readonly IDbBusinessGraphService _graph;
    private readonly IDbSyncOrchestrator _sync;
    private readonly IDbSyncScheduleService _syncSchedule;
    private readonly IDbIntelligenceInsightService _insights;
    private readonly IDbOperationService _operations;
    private readonly IDbIntelligenceTenantGuard _tenantGuard;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public DatabaseIntelligenceController(
        IDbConnectionProfileService connections,
        IDbSchemaDiscoveryService discovery,
        IBusinessDiscoveryService businessDiscovery,
        IDataHealthService health,
        IDbBusinessGraphService graph,
        IDbSyncOrchestrator sync,
        IDbSyncScheduleService syncSchedule,
        IDbIntelligenceInsightService insights,
        IDbOperationService operations,
        IDbIntelligenceTenantGuard tenantGuard,
        IHttpContextAccessor httpContextAccessor)
    {
        _connections = connections;
        _discovery = discovery;
        _businessDiscovery = businessDiscovery;
        _health = health;
        _graph = graph;
        _sync = sync;
        _syncSchedule = syncSchedule;
        _insights = insights;
        _operations = operations;
        _tenantGuard = tenantGuard;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpPost("connections")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbConnectionProfileDto>> Create(
        [FromQuery] Guid tenantId,
        [FromBody] CreateDbConnectionProfileRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            var created = await _connections.CreateAsync(
                tenantId, GetUserId(), request, GetClientIp(), GetUserAgent(), cancellationToken);
            return Ok(created);
        }
        catch (DbIntelligenceValidationException ex) { return BadRequest(ex.Message); }
        catch (DbIntelligenceQuotaException ex) { return StatusCode(StatusCodes.Status429TooManyRequests, ex.Message); }
    }

    [HttpGet("connections")]
    public async Task<ActionResult<IReadOnlyList<DbConnectionProfileDto>>> List(
        [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _connections.ListAsync(tenantId, cancellationToken));
    }

    [HttpGet("connections/{id:guid}")]
    public async Task<ActionResult<DbConnectionProfileDto>> Get(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var profile = await _connections.GetAsync(tenantId, id, cancellationToken);
        return profile == null ? NotFound() : Ok(profile);
    }

    [HttpPost("connections/{id:guid}/test")]
    public async Task<ActionResult<DbConnectionTestResultDto>> TestExisting(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _connections.TestExistingAsync(
                tenantId, GetUserId(), id, GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("connections/test")]
    public async Task<ActionResult<DbConnectionTestResultDto>> Test(
        [FromBody] TestDbConnectionRequest request, CancellationToken cancellationToken)
    {
        try { return Ok(await _connections.TestAsync(request, cancellationToken)); }
        catch (DbIntelligenceValidationException ex) { return BadRequest(ex.Message); }
    }

    [HttpDelete("connections/{id:guid}")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<IActionResult> Delete(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            await _connections.DeleteAsync(tenantId, GetUserId(), id, GetClientIp(), GetUserAgent(), cancellationToken);
            return NoContent();
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("connections/{id:guid}/discover")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbDiscoveryJobDto>> StartDiscovery(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _discovery.StartDiscoveryAsync(
                tenantId, GetUserId(), id, GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("discovery-jobs/{jobId:guid}")]
    public async Task<ActionResult<DbDiscoveryJobDto>> GetDiscoveryJob(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var job = await _discovery.GetDiscoveryJobAsync(tenantId, jobId, cancellationToken);
        return job == null ? NotFound() : Ok(job);
    }

    [HttpGet("connections/{id:guid}/catalog")]
    public async Task<ActionResult<DbCatalogSnapshotDto>> GetCatalog(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var snap = await _discovery.GetLatestCatalogForConnectionAsync(tenantId, id, cancellationToken);
        return snap == null ? NotFound() : Ok(snap);
    }

    [HttpGet("connections/{id:guid}/catalog/tables")]
    public async Task<ActionResult<IReadOnlyList<DbCatalogTableDto>>> GetCatalogTables(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _discovery.ListCatalogTablesAsync(tenantId, id, cancellationToken));
    }

    [HttpGet("connections/{id:guid}/catalog/relationships")]
    public async Task<ActionResult<IReadOnlyList<DbCatalogRelationshipDto>>> GetCatalogRelationships(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _discovery.ListCatalogRelationshipsAsync(tenantId, id, cancellationToken));
    }

    [HttpGet("business-discovery/{connectionId:guid}")]
    public async Task<ActionResult<BusinessDiscoveryResultDto>> GetBusinessDiscovery(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var result = await _businessDiscovery.GetLatestBusinessDiscoveryAsync(tenantId, connectionId, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("business-discovery/{connectionId:guid}/run")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<BusinessDiscoveryResultDto>> RunBusinessDiscovery(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _businessDiscovery.RunBusinessDiscoveryAsync(
                tenantId, GetUserId(), connectionId, GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("business-discovery/mappings")]
    public async Task<ActionResult<IReadOnlyList<DbTableBusinessMappingDto>>> ListBusinessMappings(
        [FromQuery] Guid tenantId, [FromQuery] Guid? connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _businessDiscovery.ListMappingsAsync(tenantId, connectionId, cancellationToken));
    }

    [HttpPost("business-discovery/confirm")]
    [Authorize(Roles = "Admin,Owner,Manager")]
    public async Task<ActionResult<DbTableBusinessMappingDto>> ConfirmBusinessMapping(
        [FromQuery] Guid tenantId,
        [FromBody] ConfirmBusinessMappingRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _businessDiscovery.ConfirmMappingAsync(
                tenantId, GetUserId(), request, GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (DbIntelligenceValidationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("health/run")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DataHealthResultDto>> RunHealthScan(
        [FromQuery] Guid tenantId,
        [FromBody] RunDataHealthRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        if (request == null || request.ConnectionId == Guid.Empty) return BadRequest("ConnectionId is required.");
        try
        {
            return Ok(await _health.RunHealthScanAsync(
                tenantId, GetUserId(), request.ConnectionId, request.ScanMode,
                GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("health/{jobId:guid}")]
    public async Task<ActionResult<DataHealthJobDto>> GetHealthJob(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var job = await _health.GetHealthJobAsync(tenantId, jobId, cancellationToken);
        return job == null ? NotFound() : Ok(job);
    }

    [HttpGet("health/latest")]
    public async Task<ActionResult<DataHealthResultDto>> GetLatestHealth(
        [FromQuery] Guid tenantId, [FromQuery] Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var result = await _health.GetLatestHealthResultAsync(tenantId, connectionId, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("health/findings")]
    public async Task<ActionResult<IReadOnlyList<DataHealthFindingDto>>> ListHealthFindings(
        [FromQuery] Guid tenantId, [FromQuery] Guid? connectionId, [FromQuery] string? severity,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _health.ListFindingsAsync(tenantId, connectionId, severity, cancellationToken));
    }

    [HttpPost("graph/{connectionId:guid}/build")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbBusinessGraphResultDto>> BuildGraph(
        [FromQuery] Guid tenantId,
        Guid connectionId,
        [FromBody] BuildDbBusinessGraphRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _graph.BuildGraphAsync(
                tenantId, GetUserId(), connectionId, request,
                GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("graph/{connectionId:guid}")]
    public async Task<ActionResult<DbBusinessGraphDto>> GetGraph(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var graph = await _graph.GetGraphAsync(tenantId, connectionId, cancellationToken);
        return graph == null ? NotFound() : Ok(graph);
    }

    [HttpGet("graph/{connectionId:guid}/nodes")]
    public async Task<ActionResult<IReadOnlyList<DbBusinessGraphNodeDto>>> GetGraphNodes(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _graph.GetNodesAsync(tenantId, connectionId, cancellationToken));
    }

    [HttpGet("graph/{connectionId:guid}/edges")]
    public async Task<ActionResult<IReadOnlyList<DbBusinessGraphEdgeDto>>> GetGraphEdges(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _graph.GetEdgesAsync(tenantId, connectionId, cancellationToken));
    }

    [HttpGet("graph/{connectionId:guid}/summary")]
    public async Task<ActionResult<DbBusinessGraphSummaryDto>> GetGraphSummary(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var summary = await _graph.GetSummaryAsync(tenantId, connectionId, cancellationToken);
        return summary == null ? NotFound() : Ok(summary);
    }

    [HttpGet("graph/jobs/{jobId:guid}")]
    public async Task<ActionResult<DbBusinessGraphJobDto>> GetGraphJob(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var job = await _graph.GetGraphJobAsync(tenantId, jobId, cancellationToken);
        return job == null ? NotFound() : Ok(job);
    }

    [HttpPost("graph/{connectionId:guid}/export")]
    [Authorize(Roles = "Admin,Owner,Manager")]
    public async Task<IActionResult> ExportGraph(
        [FromQuery] Guid tenantId,
        Guid connectionId,
        [FromQuery] string format,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            var result = await _graph.ExportGraphAsync(
                tenantId, GetUserId(), connectionId, format,
                GetClientIp(), GetUserAgent(), cancellationToken);
            if (result.Content == null)
                return BadRequest("Export produced no content.");
            return File(result.Content, result.ContentType, result.FileName);
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (DbIntelligenceValidationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("sync/full")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbSyncJobDto>> StartFullSync(
        [FromQuery] Guid tenantId,
        [FromBody] StartDbSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _sync.StartFullSyncAsync(
                tenantId, GetUserId(), request.ConnectionId, request.ConflictPolicy,
                GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("sync/delta")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbSyncJobDto>> StartDeltaSync(
        [FromQuery] Guid tenantId,
        [FromBody] StartDbSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _sync.StartDeltaSyncAsync(
                tenantId, GetUserId(), request.ConnectionId, request.ConflictPolicy,
                GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("sync/schedule")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbSyncScheduleDto>> CreateSyncSchedule(
        [FromQuery] Guid tenantId,
        [FromBody] ScheduleDbSyncRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _syncSchedule.CreateScheduleAsync(
                tenantId, GetUserId(), request, GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("sync/history")]
    public async Task<ActionResult<IReadOnlyList<DbSyncHistoryItemDto>>> GetSyncHistory(
        [FromQuery] Guid tenantId, [FromQuery] Guid? connectionId, [FromQuery] int take = 50,
        CancellationToken cancellationToken = default)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _sync.GetHistoryAsync(tenantId, connectionId, take, cancellationToken));
    }

    [HttpGet("sync/{id:guid}")]
    public async Task<ActionResult<DbSyncJobDto>> GetSyncJob(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var job = await _sync.GetJobAsync(tenantId, id, cancellationToken);
        return job == null ? NotFound() : Ok(job);
    }

    [HttpPost("sync/{id:guid}/rollback")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbSyncRollbackResultDto>> RollbackSync(
        [FromQuery] Guid tenantId, Guid id, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _sync.RollbackJobAsync(
                tenantId, GetUserId(), id, GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("insights/generate")]
    [Authorize(Roles = "Admin,Owner,Manager")]
    public async Task<ActionResult<DbIntelligenceInsightResultDto>> GenerateInsights(
        [FromQuery] Guid tenantId,
        [FromQuery] Guid connectionId,
        [FromBody] GenerateDbIntelligenceInsightsRequest? request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _insights.GenerateInsightsAsync(
                tenantId, GetUserId(), connectionId, request,
                GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpGet("insights/{connectionId:guid}")]
    public async Task<ActionResult<IReadOnlyList<DbIntelligenceInsightDto>>> ListInsights(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        return Ok(await _insights.ListInsightsAsync(tenantId, connectionId, cancellationToken));
    }

    [HttpGet("insights/{connectionId:guid}/latest")]
    public async Task<ActionResult<DbIntelligenceInsightResultDto>> GetLatestInsights(
        [FromQuery] Guid tenantId, Guid connectionId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var result = await _insights.GetLatestInsightsAsync(tenantId, connectionId, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpGet("insights/jobs/{jobId:guid}")]
    public async Task<ActionResult<DbIntelligenceInsightJobDto>> GetInsightJob(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var job = await _insights.GetInsightJobAsync(tenantId, jobId, cancellationToken);
        return job == null ? NotFound() : Ok(job);
    }

    [HttpPost("operations/start")]
    [Authorize(Roles = "Admin,Owner,Manager")]
    public async Task<ActionResult<DbOperationJobDto>> StartOperationSession(
        [FromQuery] Guid tenantId,
        [FromBody] StartDbOperationSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        if (request.ConnectionId == Guid.Empty) return BadRequest("ConnectionId is required.");
        try
        {
            return Ok(await _operations.StartSessionAsync(
                tenantId, GetUserId(), request.ConnectionId,
                GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
        catch (InvalidOperationException ex) { return BadRequest(ex.Message); }
    }

    [HttpPost("operations/{jobId:guid}/preview")]
    [Authorize(Roles = "Admin,Owner,Manager")]
    public async Task<ActionResult<DbOperationPreviewResultDto>> PreviewOperation(
        [FromQuery] Guid tenantId, Guid jobId,
        [FromBody] DbOperationActionPlan plan,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _operations.PreviewAsync(tenantId, jobId, plan, cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpPost("operations/{jobId:guid}/execute")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbOperationResultDto>> ExecuteOperation(
        [FromQuery] Guid tenantId, Guid jobId,
        [FromBody] DbOperationActionPlan plan,
        CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _operations.ExecuteAsync(
                tenantId, GetUserId(), jobId, plan,
                GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    [HttpGet("operations/{jobId:guid}")]
    public async Task<ActionResult<DbOperationJobDto>> GetOperationJob(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var job = await _operations.GetJobAsync(tenantId, jobId, cancellationToken);
        return job == null ? NotFound() : Ok(job);
    }

    [HttpGet("operations/{jobId:guid}/result")]
    public async Task<ActionResult<DbOperationResultDto>> GetOperationResult(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        var result = await _operations.GetResultAsync(tenantId, jobId, cancellationToken);
        return result == null ? NotFound() : Ok(result);
    }

    [HttpPost("operations/{jobId:guid}/rollback")]
    [Authorize(Roles = "Admin,Owner")]
    public async Task<ActionResult<DbOperationRollbackResultDto>> RollbackOperation(
        [FromQuery] Guid tenantId, Guid jobId, CancellationToken cancellationToken)
    {
        if (!_tenantGuard.IsSameTenant(tenantId)) return Forbid();
        try
        {
            return Ok(await _operations.RollbackAsync(
                tenantId, GetUserId(), jobId, GetClientIp(), GetUserAgent(), cancellationToken));
        }
        catch (KeyNotFoundException) { return NotFound(); }
    }

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private string? GetClientIp() => _httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    private string? GetUserAgent() => _httpContextAccessor.HttpContext?.Request.Headers.UserAgent.ToString();
}
