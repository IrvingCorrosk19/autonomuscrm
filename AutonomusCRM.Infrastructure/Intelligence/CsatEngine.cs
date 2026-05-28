using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class CsatEngine : ICsatEngine
{
    private readonly ICustomerFeedbackRepository _feedbackRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CsatEngine(ICustomerFeedbackRepository feedbackRepository, IUnitOfWork unitOfWork)
    {
        _feedbackRepository = feedbackRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<CsatSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var responses = (await _feedbackRepository.GetByTenantAsync(tenantId, cancellationToken))
            .Where(f => f.FeedbackType == IntelligenceConstants.FeedbackCsat)
            .OrderBy(f => f.SubmittedAt)
            .ToList();

        var avg = responses.Any() ? responses.Average(r => r.Score) : 0;
        var last30 = responses.Where(r => r.SubmittedAt >= DateTime.UtcNow.AddDays(-30)).ToList();
        var prev30 = responses.Where(r => r.SubmittedAt >= DateTime.UtcNow.AddDays(-60) && r.SubmittedAt < DateTime.UtcNow.AddDays(-30)).ToList();
        var trend = (last30.Any() ? last30.Average(r => r.Score) : 0)
                    - (prev30.Any() ? prev30.Average(r => r.Score) : last30.Any() ? last30.Average(r => r.Score) : 0);

        var history = responses
            .GroupBy(r => new DateTime(r.SubmittedAt.Year, r.SubmittedAt.Month, 1))
            .Select(g => new CsatTrendPointDto(g.Key, Math.Round(g.Average(x => x.Score), 2), g.Count()))
            .Take(12)
            .ToList();

        return new CsatSummaryDto(Math.Round(avg, 2), Math.Round(trend, 2), responses.Count, history);
    }

    public async Task<Guid> SubmitCsatAsync(
        Guid tenantId, Guid customerId, int score, string? comment = null, CancellationToken cancellationToken = default)
    {
        var feedback = CustomerFeedback.Create(tenantId, customerId, IntelligenceConstants.FeedbackCsat, score, comment);
        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return feedback.Id;
    }
}
