using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class CustomerDataMartService : ICustomerDataMartService
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerAnalyticsSnapshotRepository _snapshotRepository;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IChurnPredictionV2 _churnPrediction;
    private readonly INpsEngine _npsEngine;
    private readonly ICsatEngine _csatEngine;
    private readonly ICustomerFeedbackRepository _feedbackRepository;
    private readonly ICustomerSegmentationEngine _segmentation;
    private readonly IExpansionIntelligence _expansionIntel;
    private readonly IUnitOfWork _unitOfWork;

    public CustomerDataMartService(
        ICustomerRepository customerRepository,
        ICustomerAnalyticsSnapshotRepository snapshotRepository,
        ICustomerHealthEngine healthEngine,
        IChurnPredictionV2 churnPrediction,
        INpsEngine npsEngine,
        ICsatEngine csatEngine,
        ICustomerFeedbackRepository feedbackRepository,
        ICustomerSegmentationEngine segmentation,
        IExpansionIntelligence expansionIntel,
        IUnitOfWork unitOfWork)
    {
        _customerRepository = customerRepository;
        _snapshotRepository = snapshotRepository;
        _healthEngine = healthEngine;
        _churnPrediction = churnPrediction;
        _npsEngine = npsEngine;
        _csatEngine = csatEngine;
        _feedbackRepository = feedbackRepository;
        _segmentation = segmentation;
        _expansionIntel = expansionIntel;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> BuildDailySnapshotsAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP or CustomerStatus.Qualified)
            .ToList();

        var healthList = (await _healthEngine.CalculateAllAsync(tenantId, cancellationToken))
            .ToDictionary(h => h.CustomerId);
        var churnList = (await _churnPrediction.PredictAsync(tenantId, cancellationToken: cancellationToken))
            .ToDictionary(c => c.CustomerId);
        var expansionList = (await _expansionIntel.AnalyzeAsync(tenantId, cancellationToken))
            .ToDictionary(e => e.CustomerId);
        var feedback = (await _feedbackRepository.GetByTenantAsync(tenantId, cancellationToken)).ToList();

        var created = 0;
        foreach (var customer in customers)
        {
            var existing = await _snapshotRepository.GetLatestAsync(tenantId, customer.Id, cancellationToken);
            if (existing?.SnapshotDate == today)
                continue;

            healthList.TryGetValue(customer.Id, out var health);
            churnList.TryGetValue(customer.Id, out var churn);
            expansionList.TryGetValue(customer.Id, out var expansion);

            var npsLatest = feedback
                .Where(f => f.CustomerId == customer.Id && f.FeedbackType == IntelligenceConstants.FeedbackNps)
                .OrderByDescending(f => f.SubmittedAt).FirstOrDefault()?.Score;
            var csatLatest = feedback
                .Where(f => f.CustomerId == customer.Id && f.FeedbackType == IntelligenceConstants.FeedbackCsat)
                .OrderByDescending(f => f.SubmittedAt).FirstOrDefault()?.Score;

            var segment = await _segmentation.ResolveSegmentAsync(tenantId, customer.Id, cancellationToken);

            var snapshot = CustomerAnalyticsSnapshot.Create(
                tenantId,
                customer.Id,
                today,
                health?.HealthScore ?? 50,
                churn?.ChurnProbability ?? customer.RiskScore ?? 50,
                npsLatest,
                csatLatest.HasValue ? (decimal)csatLatest.Value : null,
                customer.LifetimeValue ?? 0,
                expansion?.ReadinessScore ?? 0,
                segment,
                health?.EngagementScore ?? 0,
                health?.AdoptionScore ?? 0,
                1);

            await _snapshotRepository.AddAsync(snapshot, cancellationToken);
            created++;
        }

        if (created > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);

        return created;
    }

    public async Task<IReadOnlyList<CustomerSnapshotTrendDto>> GetTrendsAsync(
        Guid tenantId, Guid? customerId = null, CancellationToken cancellationToken = default)
    {
        var from = DateTime.UtcNow.AddDays(-90);
        var snapshots = customerId.HasValue
            ? (await _snapshotRepository.GetByCustomerAsync(tenantId, customerId.Value, 90, cancellationToken)).ToList()
            : (await _snapshotRepository.GetByTenantAsync(tenantId, from, cancellationToken)).ToList();

        return snapshots
            .GroupBy(s => s.CustomerId)
            .Select(g => new CustomerSnapshotTrendDto(
                g.Key,
                g.OrderBy(s => s.SnapshotDate)
                    .Select(s => new SnapshotPointDto(s.SnapshotDate, s.HealthScore, s.ChurnRiskScore, s.NpsScore, s.CsatScore))
                    .ToList()))
            .Take(customerId.HasValue ? 1 : 50)
            .ToList();
    }
}
