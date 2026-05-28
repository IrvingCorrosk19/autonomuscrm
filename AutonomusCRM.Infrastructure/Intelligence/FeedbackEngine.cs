using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Intelligence;

namespace AutonomusCRM.Infrastructure.Intelligence;

public class FeedbackEngine : IFeedbackEngine
{
    private readonly ICustomerFeedbackRepository _feedbackRepository;
    private readonly INpsEngine _npsEngine;
    private readonly ICsatEngine _csatEngine;
    private readonly IUnitOfWork _unitOfWork;

    public FeedbackEngine(
        ICustomerFeedbackRepository feedbackRepository,
        INpsEngine npsEngine,
        ICsatEngine csatEngine,
        IUnitOfWork unitOfWork)
    {
        _feedbackRepository = feedbackRepository;
        _npsEngine = npsEngine;
        _csatEngine = csatEngine;
        _unitOfWork = unitOfWork;
    }

    public async Task<FeedbackSummaryDto> GetSummaryAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var all = (await _feedbackRepository.GetByTenantAsync(tenantId, cancellationToken)).ToList();
        var comments = all
            .Where(f => f.FeedbackType == IntelligenceConstants.FeedbackComment
                        || !string.IsNullOrWhiteSpace(f.Comment))
            .OrderByDescending(f => f.SubmittedAt)
            .Take(25)
            .Select(f => new FeedbackItemDto(f.Id, f.CustomerId, f.FeedbackType, f.Score, f.Comment, f.SubmittedAt))
            .ToList();

        return new FeedbackSummaryDto(
            all.Count,
            await _npsEngine.GetSummaryAsync(tenantId, cancellationToken),
            await _csatEngine.GetSummaryAsync(tenantId, cancellationToken),
            comments);
    }

    public async Task<Guid> SubmitCommentAsync(
        Guid tenantId, Guid customerId, string comment, CancellationToken cancellationToken = default)
    {
        var feedback = CustomerFeedback.Create(tenantId, customerId, IntelligenceConstants.FeedbackComment, 0, comment);
        await _feedbackRepository.AddAsync(feedback, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return feedback.Id;
    }
}
