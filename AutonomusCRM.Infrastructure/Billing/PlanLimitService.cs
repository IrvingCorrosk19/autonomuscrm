using AutonomusCRM.Application.Billing;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace AutonomusCRM.Infrastructure.Billing;

public sealed class PlanLimitService : IPlanLimitService
{
    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public PlanLimitService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<PlanLimitsProfile> GetLimitsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var account = await _db.TenantBillingAccounts.FirstOrDefaultAsync(x => x.TenantId == tenantId, cancellationToken);
        return PlanLimitsProfile.ForPlan(account?.PlanId ?? BillingPlans.Free);
    }

    public async Task<PlanLimitCheckResult> CheckAsync(Guid tenantId, string resource, CancellationToken cancellationToken = default)
    {
        var limits = await GetLimitsAsync(tenantId, cancellationToken);
        return resource.ToLowerInvariant() switch
        {
            "users" => CountCheck(await _db.Users.CountAsync(u => u.TenantId == tenantId, cancellationToken), limits.MaxUsers, "PLAN_LIMIT_USERS"),
            "customers" => CountCheck(await _db.Customers.CountAsync(c => c.TenantId == tenantId, cancellationToken), limits.MaxCustomers, "PLAN_LIMIT_CUSTOMERS"),
            "leads" => CountCheck(await _db.Leads.CountAsync(l => l.TenantId == tenantId, cancellationToken), limits.MaxLeads, "PLAN_LIMIT_LEADS"),
            "deals" => CountCheck(await _db.Deals.CountAsync(d => d.TenantId == tenantId, cancellationToken), limits.MaxDeals, "PLAN_LIMIT_DEALS"),
            "integrations" => CountCheck(await _db.TenantIntegrations.CountAsync(i => i.TenantId == tenantId && i.IsEnabled, cancellationToken), limits.MaxIntegrations, "PLAN_LIMIT_INTEGRATIONS"),
            "ai_agents" => new PlanLimitCheckResult(true, null, null, 0, limits.MaxAiAgents),
            "api_calls" => CheckApiCalls(tenantId, limits.MaxApiCallsPerDay),
            _ => new PlanLimitCheckResult(true, null, null, 0, 0)
        };
    }

    public Task RecordApiCallAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var key = ApiKey(tenantId);
        _cache.GetOrCreate(key, e =>
        {
            e.AbsoluteExpiration = DateTimeOffset.UtcNow.Date.AddDays(1);
            return 0;
        });
        if (_cache.TryGetValue(key, out int count))
            _cache.Set(key, count + 1, DateTimeOffset.UtcNow.Date.AddDays(1));
        return Task.CompletedTask;
    }

    private PlanLimitCheckResult CheckApiCalls(Guid tenantId, int limit)
    {
        var key = ApiKey(tenantId);
        var current = _cache.TryGetValue(key, out int c) ? c : 0;
        if (current >= limit)
            return new PlanLimitCheckResult(false, "PLAN_LIMIT_API", $"Límite diario de API alcanzado ({current}/{limit}). Actualiza tu plan.", current, limit);
        return new PlanLimitCheckResult(true, null, null, current, limit);
    }

    private static PlanLimitCheckResult CountCheck(int current, int limit, string code)
    {
        if (current >= limit)
            return new PlanLimitCheckResult(false, code,
                $"Has alcanzado el límite de tu plan ({current}/{limit}). Mejora el plan para continuar.", current, limit);
        return new PlanLimitCheckResult(true, null, null, current, limit);
    }

    private static string ApiKey(Guid tenantId) => $"plan:api:{tenantId}:{DateTime.UtcNow:yyyyMMdd}";
}
