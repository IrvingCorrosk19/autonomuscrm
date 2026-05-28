using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.EnterpriseAI;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Infrastructure.Autonomous;

public class MlFoundationService : IMlFoundationService
{
    private static readonly string[] Datasets = EnterpriseAiConstants.DatasetTypes;

    private readonly IMlFeatureSnapshotRepository _snapshots;
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerHealthEngine _healthEngine;
    private readonly IChurnPredictionV2 _churn;
    private readonly ICustomerFeedbackRepository _feedback;
    private readonly IExpansionIntelligence _expansion;
    private readonly IRenewalEngine _renewal;
    private readonly IDealRepository _deals;
    private readonly IUnitOfWork _unitOfWork;

    public MlFoundationService(
        IMlFeatureSnapshotRepository snapshots,
        ICustomerRepository customerRepository,
        ICustomerHealthEngine healthEngine,
        IChurnPredictionV2 churn,
        ICustomerFeedbackRepository feedback,
        IExpansionIntelligence expansion,
        IRenewalEngine renewal,
        IDealRepository deals,
        IUnitOfWork unitOfWork)
    {
        _snapshots = snapshots;
        _customerRepository = customerRepository;
        _healthEngine = healthEngine;
        _churn = churn;
        _feedback = feedback;
        _expansion = expansion;
        _renewal = renewal;
        _deals = deals;
        _unitOfWork = unitOfWork;
    }

    public async Task<int> CaptureTrainingSamplesAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => c.Status is CustomerStatus.Customer or CustomerStatus.VIP or CustomerStatus.Churned).ToList();
        var health = (await _healthEngine.CalculateAllAsync(tenantId, cancellationToken)).ToDictionary(h => h.CustomerId);
        var churn = (await _churn.PredictAsync(tenantId, cancellationToken: cancellationToken)).ToDictionary(c => c.CustomerId);
        var expansion = (await _expansion.AnalyzeAsync(tenantId, cancellationToken)).ToDictionary(e => e.CustomerId);
        var renewals = (await _renewal.GetUpcomingRenewalsAsync(tenantId, cancellationToken)).ToDictionary(r => r.CustomerId);
        var deals = (await _deals.GetByTenantIdAsync(tenantId, cancellationToken)).ToList();
        var feedback = (await _feedback.GetByTenantAsync(tenantId, cancellationToken)).ToList();
        var created = 0;

        foreach (var customer in customers)
        {
            health.TryGetValue(customer.Id, out var h);
            churn.TryGetValue(customer.Id, out var ch);
            var nps = feedback.Where(f => f.CustomerId == customer.Id && f.FeedbackType == IntelligenceConstants.FeedbackNps).OrderByDescending(f => f.SubmittedAt).FirstOrDefault();
            var csat = feedback.Where(f => f.CustomerId == customer.Id && f.FeedbackType == IntelligenceConstants.FeedbackCsat).OrderByDescending(f => f.SubmittedAt).FirstOrDefault();

            var features = new Dictionary<string, object>
            {
                ["health"] = h?.HealthScore ?? 50,
                ["engagement"] = h?.EngagementScore ?? 50,
                ["adoption"] = h?.AdoptionScore ?? 50,
                ["ltv"] = customer.LifetimeValue ?? 0,
                ["risk"] = customer.RiskScore ?? 50,
                ["churn_prob"] = ch?.ChurnProbability ?? 0
            };

            var churnLabel = customer.Status == CustomerStatus.Churned || (ch?.ChurnProbability ?? 0) >= 80 ? "churned" : "retained";
            await _snapshots.AddAsync(MlFeatureSnapshot.Capture(tenantId, "churn", features, churnLabel, customer.Id), cancellationToken);
            created++;

            if (nps != null)
            {
                var nf = new Dictionary<string, object>(features) { ["nps"] = nps.Score };
                await _snapshots.AddAsync(MlFeatureSnapshot.Capture(tenantId, "nps", nf, INpsEngine.Classify(nps.Score), customer.Id), cancellationToken);
                created++;
            }

            if (csat != null)
            {
                var cf = new Dictionary<string, object>(features) { ["csat"] = csat.Score };
                await _snapshots.AddAsync(MlFeatureSnapshot.Capture(tenantId, "csat", cf, csat.Score >= 4 ? "satisfied" : "unsatisfied", customer.Id), cancellationToken);
                created++;
            }

            expansion.TryGetValue(customer.Id, out var exp);
            var expFeatures = new Dictionary<string, object>(features) { ["expansion_score"] = exp?.ReadinessScore ?? 0 };
            var expLabel = (exp?.ReadinessScore ?? 0) >= 60 ? "expansion_ready" : "not_ready";
            await _snapshots.AddAsync(MlFeatureSnapshot.Capture(tenantId, "expansion", expFeatures, expLabel, customer.Id), cancellationToken);
            created++;

            renewals.TryGetValue(customer.Id, out var ren);
            var renFeatures = new Dictionary<string, object>(features)
            {
                ["renewal_days"] = ren != null ? (ren.RenewalDate - DateTime.UtcNow).TotalDays : 365
            };
            await _snapshots.AddAsync(MlFeatureSnapshot.Capture(tenantId, "renewal", renFeatures, ren != null ? "renewed" : "pending", customer.Id), cancellationToken);
            created++;

            var engFeatures = new Dictionary<string, object>(features) { ["engagement"] = h?.EngagementScore ?? 50 };
            await _snapshots.AddAsync(MlFeatureSnapshot.Capture(tenantId, "engagement", engFeatures, (h?.EngagementScore ?? 0) >= 50 ? "engaged" : "disengaged", customer.Id), cancellationToken);
            created++;
        }

        var wonDeals = deals.Where(d => d.Stage == DealStage.ClosedWon && d.ClosedAt >= DateTime.UtcNow.AddMonths(-6)).ToList();
        foreach (var d in wonDeals.Take(50))
        {
            var revFeatures = new Dictionary<string, object>
            {
                ["deal_value"] = (double)d.Amount,
                ["revenue_velocity"] = (double)d.Amount
            };
            await _snapshots.AddAsync(MlFeatureSnapshot.Capture(tenantId, "revenue", revFeatures, d.Amount >= 10000 ? "high_revenue" : "standard", d.CustomerId), cancellationToken);
            created++;
        }

        if (created > 0)
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        return created;
    }

    public async Task<IReadOnlyList<MlDatasetSummaryDto>> GetDatasetSummaryAsync(
        Guid tenantId, CancellationToken cancellationToken = default)
    {
        var result = new List<MlDatasetSummaryDto>();
        foreach (var ds in Datasets)
        {
            var count = await _snapshots.CountByDatasetAsync(tenantId, ds, cancellationToken);
            var latest = (await _snapshots.GetByDatasetAsync(tenantId, ds, 1, cancellationToken)).FirstOrDefault();
            result.Add(new MlDatasetSummaryDto(ds, count, latest?.CapturedAt));
        }
        return result;
    }
}
