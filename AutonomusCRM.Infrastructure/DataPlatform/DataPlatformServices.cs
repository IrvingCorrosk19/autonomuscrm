using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataPlatform;

public sealed class Customer360Service : ICustomer360Service
{
    private readonly ApplicationDbContext _db;
    private readonly IDealRepository _deals;
    private readonly IChurnPredictionV2 _churn;

    public Customer360Service(ApplicationDbContext db, IDealRepository deals, IChurnPredictionV2 churn)
    {
        _db = db;
        _deals = deals;
        _churn = churn;
    }

    public async Task<Customer360Dto?> GetAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default)
    {
        var customer = await _db.Customers.FirstOrDefaultAsync(c => c.Id == customerId && c.TenantId == tenantId, cancellationToken);
        if (customer == null) return null;

        var deals = (await _deals.GetAllAsync(cancellationToken)).Where(d => d.CustomerId == customerId).ToList();
        var openPipeline = deals.Where(d => d.Stage != Domain.Deals.DealStage.ClosedWon && d.Stage != Domain.Deals.DealStage.ClosedLost)
            .Sum(d => d.Amount);
        var won = deals.Where(d => d.Stage == Domain.Deals.DealStage.ClosedWon).Sum(d => d.Amount);

        var since = DateTime.UtcNow.AddDays(-30);
        var usage = await _db.ProductUsageEvents.CountAsync(
            e => e.TenantId == tenantId && e.CustomerId == customerId && e.RecordedAt >= since, cancellationToken);

        var churnList = await _churn.PredictAsync(tenantId, customerId, cancellationToken);
        var churnRisk = churnList.FirstOrDefault() is { } c ? (double?)c.ChurnProbability : null;

        var audits = await _db.AiDecisionAudits
            .Where(a => a.TenantId == tenantId && a.CustomerId == customerId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .Select(a => a.Action)
            .ToListAsync(cancellationToken);

        return new Customer360Dto(customer.Id, customer.Name, customer.Email, openPipeline, won, usage,
            churnRisk, audits);
    }

    public async Task<IReadOnlyList<Customer360Dto>> SearchAsync(Guid tenantId, string? query, int take, CancellationToken cancellationToken = default)
    {
        var q = _db.Customers.Where(c => c.TenantId == tenantId);
        if (!string.IsNullOrWhiteSpace(query))
            q = q.Where(c => c.Name.Contains(query) || (c.Email != null && c.Email.Contains(query)));

        var ids = await q.OrderBy(c => c.Name).Take(take).Select(c => c.Id).ToListAsync(cancellationToken);
        var results = new List<Customer360Dto>();
        foreach (var id in ids)
        {
            var dto = await GetAsync(tenantId, id, cancellationToken);
            if (dto != null) results.Add(dto);
        }

        return results;
    }
}

public sealed class DataAcquisitionService : IDataAcquisitionService
{
    private readonly Application.Common.Imports.ICrmImportService _import;

    public DataAcquisitionService(Application.Common.Imports.ICrmImportService import) => _import = import;

    public async Task<DataIngestResultDto> IngestWebhookBatchAsync(
        Guid tenantId, string entityType, IReadOnlyList<Dictionary<string, object?>> records,
        CancellationToken cancellationToken = default)
    {
        var deduped = records
            .GroupBy(r => r.TryGetValue("email", out var e) ? e?.ToString()?.ToLowerInvariant() ?? Guid.NewGuid().ToString() : Guid.NewGuid().ToString())
            .Select(g => g.First())
            .ToList();

        var normalized = deduped.Select(r => new Application.Common.Imports.CustomerImportRow(
            r.TryGetValue("name", out var n) ? n?.ToString() ?? "Unknown" : "Unknown",
            r.TryGetValue("email", out var em) ? em?.ToString() : null,
            r.TryGetValue("phone", out var ph) ? ph?.ToString() : null,
            r.TryGetValue("company", out var co) ? co?.ToString() : null)).ToList();

        if (!string.Equals(entityType, "customers", StringComparison.OrdinalIgnoreCase))
            return new DataIngestResultDto(0, deduped.Count, normalized.Count);

        var result = await _import.ImportCustomersAsync(tenantId, normalized, cancellationToken);
        return new DataIngestResultDto(result.Created, records.Count - deduped.Count, normalized.Count);
    }
}

public sealed class MarketplaceCatalogService : IMarketplaceCatalogService
{
    public IReadOnlyList<MarketplaceExtensionDto> ListExtensions() => new[]
    {
        new MarketplaceExtensionDto("hubspot-sync", "HubSpot Sync", "1.0.0", new[] { "crm.read", "crm.write" }, "stable"),
        new MarketplaceExtensionDto("salesforce-sync", "Salesforce Sync", "1.0.0", new[] { "crm.read", "crm.write" }, "stable"),
        new MarketplaceExtensionDto("stripe-billing", "Stripe Billing", "1.0.0", new[] { "billing.read", "billing.write" }, "stable"),
        new MarketplaceExtensionDto("revenue-autopilot", "Revenue Autopilot", "1.0.0", new[] { "ai.execute", "ai.audit" }, "beta")
    };
}
