using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Intelligence;
using AutonomusCRM.Domain.Customers;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class NpsEngine : INpsEngine
{
    private readonly ICustomerFeedbackRepository _feedbackRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IUnitOfWork _unitOfWork;

    public NpsEngine(
        ICustomerFeedbackRepository feedbackRepository,
        ICustomerRepository customerRepository,
        IUnitOfWork unitOfWork)
    {
        _feedbackRepository = feedbackRepository;
        _customerRepository = customerRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<NpsSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var responses = (await _feedbackRepository.GetByTenantAsync(tenantId, cancellationToken))
            .Where(f => f.FeedbackType == IntelligenceConstants.FeedbackNps).ToList();
        var customers = (await _customerRepository.GetByTenantIdAsync(tenantId, cancellationToken))
            .ToDictionary(c => c.Id, c => c.Name);

        var promoters = responses.Count(r => INpsEngine.Classify(r.Score) == IntelligenceConstants.NpsPromoter);
        var passives = responses.Count(r => INpsEngine.Classify(r.Score) == IntelligenceConstants.NpsPassive);
        var detractors = responses.Count(r => INpsEngine.Classify(r.Score) == IntelligenceConstants.NpsDetractor);
        var total = responses.Count;
        var globalNps = total > 0 ? Math.Round((promoters - detractors) * 100.0 / total, 1) : 0;

        var byCustomer = responses
            .GroupBy(r => r.CustomerId)
            .Select(g =>
            {
                var latest = g.OrderByDescending(x => x.SubmittedAt).First();
                customers.TryGetValue(latest.CustomerId, out var name);
                return new NpsByCustomerDto(
                    latest.CustomerId, name ?? "Cliente", latest.Score,
                    INpsEngine.Classify(latest.Score), latest.SubmittedAt);
            }).Take(50).ToList();

        var bySegment = responses
            .Where(r => !string.IsNullOrWhiteSpace(r.Segment))
            .GroupBy(r => r.Segment!)
            .Select(g =>
            {
                var p = g.Count(x => INpsEngine.Classify(x.Score) == IntelligenceConstants.NpsPromoter);
                var d = g.Count(x => INpsEngine.Classify(x.Score) == IntelligenceConstants.NpsDetractor);
                var nps = g.Count() > 0 ? Math.Round((p - d) * 100.0 / g.Count(), 1) : 0;
                return new NpsBySegmentDto(g.Key, nps, g.Count());
            }).ToList();

        return new NpsSummaryDto(globalNps, promoters, passives, detractors, total, byCustomer, bySegment);
    }

    public async Task<Guid> SubmitNpsAsync(
        Guid tenantId, Guid customerId, int score, string? comment = null, CancellationToken cancellationToken = default)
    {
        var feedback = CustomerFeedback.Create(tenantId, customerId, IntelligenceConstants.FeedbackNps, score, comment);
        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return feedback.Id;
    }
}
