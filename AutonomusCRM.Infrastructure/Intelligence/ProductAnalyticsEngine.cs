using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class ProductAnalyticsEngine : IProductAnalyticsEngine
{
    private readonly IProductUsageEventRepository _usageRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public ProductAnalyticsEngine(
        IProductUsageEventRepository usageRepository,
        IUserRepository userRepository,
        IUnitOfWork unitOfWork)
    {
        _usageRepository = usageRepository;
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ProductAnalyticsDto> GetAnalyticsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var events = (await _usageRepository.GetByTenantAsync(tenantId, now.AddDays(-90), now, cancellationToken)).ToList();
        var users = (await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken)).Where(u => u.IsActive).ToList();

        var dauUsers = users.Count(u => u.LastLoginAt >= now.AddDays(-1));
        var wauUsers = users.Count(u => u.LastLoginAt >= now.AddDays(-7));
        var mauUsers = users.Count(u => u.LastLoginAt >= now.AddDays(-30));

        var loginEvents = events.Where(e => e.EventType == IntelligenceConstants.UsageLogin).ToList();
        var sessionEvents = events.Where(e => e.EventType == IntelligenceConstants.UsageSession).ToList();

        var dau = Math.Max(dauUsers, loginEvents.Count(e => e.RecordedAt >= now.AddDays(-1)));
        var wau = Math.Max(wauUsers, loginEvents.Select(e => e.UserId).Distinct().Count());
        var mau = Math.Max(mauUsers, loginEvents.Select(e => e.UserId).Distinct().Count());
        var stickiness = mau > 0 ? Math.Round(dau * 100.0 / mau, 1) : 0;

        var avgSession = sessionEvents.Any()
            ? sessionEvents.Average(e => e.DurationMinutes > 0 ? e.DurationMinutes : 15)
            : 0;

        var byModule = events.GroupBy(e => e.Module)
            .ToDictionary(g => g.Key, g => g.Count());

        foreach (var mod in IntelligenceConstants.CrmModules.Where(m => !byModule.ContainsKey(m)))
            byModule[mod] = 0;

        return new ProductAnalyticsDto(
            dau, wau, mau, stickiness, Math.Round(avgSession, 1),
            byModule, loginEvents.Count, users.Count);
    }

    public async Task RecordUsageAsync(
        Guid tenantId, string module, string eventType,
        Guid? userId = null, Guid? customerId = null, int durationMinutes = 0,
        CancellationToken cancellationToken = default)
    {
        var evt = ProductUsageEvent.Create(tenantId, module, eventType, userId, customerId, durationMinutes);
        await _usageRepository.AddAsync(evt, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task SyncFromUserLoginsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var users = await _userRepository.GetByTenantIdAsync(tenantId, cancellationToken);
        var since = DateTime.UtcNow.AddDays(-1);
        foreach (var user in users.Where(u => u.LastLoginAt >= since))
        {
            await RecordUsageAsync(tenantId, "Auth", IntelligenceConstants.UsageLogin, user.Id, cancellationToken: cancellationToken);
        }
    }
}
