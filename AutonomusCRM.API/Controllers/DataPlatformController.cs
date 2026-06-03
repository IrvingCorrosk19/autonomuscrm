using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataPlatform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/data")]
public class DataPlatformController : ControllerBase
{
    private readonly ICustomer360Service _customer360;
    private readonly IDataAcquisitionService _acquisition;
    private readonly IIdentityResolutionService _identity;
    private readonly IIdentityMergeService _merge;
    private readonly IWarehouseExportService _warehouse;
    private readonly ICdpEventStreamService _stream;
    private readonly ITenantContext _tenant;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DataPlatformController> _logger;

    public DataPlatformController(
        ICustomer360Service customer360,
        IDataAcquisitionService acquisition,
        IIdentityResolutionService identity,
        IIdentityMergeService merge,
        IWarehouseExportService warehouse,
        ICdpEventStreamService stream,
        ITenantContext tenant,
        IConfiguration configuration,
        ILogger<DataPlatformController> logger)
    {
        _customer360 = customer360;
        _acquisition = acquisition;
        _identity = identity;
        _merge = merge;
        _warehouse = warehouse;
        _stream = stream;
        _tenant = tenant;
        _configuration = configuration;
        _logger = logger;
    }

    [HttpGet("identity/duplicates")]
    public async Task<IActionResult> IdentityDuplicates(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _identity.FindDuplicatesByEmailAsync(tenantId, cancellationToken));
    }

    [HttpPost("identity/merge")]
    public async Task<IActionResult> MergeDuplicates(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var merged = await _merge.MergeDuplicatesAsync(tenantId, cancellationToken);
        return Ok(new { merged });
    }

    [HttpGet("warehouse/export/customers.csv")]
    public async Task<IActionResult> ExportCustomers(CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var bytes = await _warehouse.ExportCustomersCsvAsync(tenantId, cancellationToken);
        _logger.LogInformation("Customer CSV export audit: TenantId={TenantId} User={User} Bytes={Bytes}",
            tenantId, User.Identity?.Name ?? "unknown", bytes.Length);
        return File(bytes, "text/csv", $"customers-{tenantId:N}.csv");
    }

    [HttpGet("stream")]
    public async Task<IActionResult> EventStream([FromQuery] int take = 100, CancellationToken cancellationToken = default)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _stream.GetRecentAsync(tenantId, take, cancellationToken));
    }

    [HttpGet("customer360/{customerId:guid}")]
    public async Task<IActionResult> Customer360(Guid customerId, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        var dto = await _customer360.GetAsync(tenantId, customerId, cancellationToken);
        return dto == null ? NotFound() : Ok(dto);
    }

    [HttpGet("customer360")]
    public async Task<IActionResult> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        var tenantId = _tenant.TenantId ?? throw new InvalidOperationException("Tenant required");
        return Ok(await _customer360.SearchAsync(tenantId, q, 20, cancellationToken));
    }

    [HttpPost("ingest/{tenantId:guid}/{entityType}")]
    [AllowAnonymous]
    public async Task<IActionResult> Ingest(
        Guid tenantId,
        string entityType,
        [FromBody] List<Dictionary<string, object?>> records,
        CancellationToken cancellationToken)
    {
        var ingestKey = _configuration["DataPlatform:IngestApiKey"];
        if (string.IsNullOrWhiteSpace(ingestKey))
            return StatusCode(StatusCodes.Status503ServiceUnavailable, new { error = "Data ingest not configured." });

        if (!Request.Headers.TryGetValue("X-Data-Ingest-Key", out var provided) || provided != ingestKey)
            return Unauthorized(new { error = "Invalid or missing X-Data-Ingest-Key." });

        return Ok(await _acquisition.IngestWebhookBatchAsync(tenantId, entityType, records, cancellationToken));
    }
}
