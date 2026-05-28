using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Revenue.Commands;

public record UpsertSalesQuotaCommand(
    Guid TenantId,
    Guid UserId,
    string PeriodType,
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal TargetAmount,
    Guid? QuotaId = null) : IRequest<Guid>;

public class UpsertSalesQuotaCommandHandler : IRequestHandler<UpsertSalesQuotaCommand, Guid>
{
    private readonly ISalesQuotaRepository _repository;
    private readonly IUnitOfWork _unitOfWork;

    public UpsertSalesQuotaCommandHandler(ISalesQuotaRepository repository, IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Guid> HandleAsync(UpsertSalesQuotaCommand request, CancellationToken cancellationToken = default)
    {
        if (request.QuotaId.HasValue)
        {
            var existing = await _repository.GetByIdAsync(request.QuotaId.Value, cancellationToken);
            if (existing != null && existing.TenantId == request.TenantId)
            {
                existing.UpdateTarget(request.TargetAmount, request.PeriodEnd);
                await _repository.UpdateAsync(existing, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return existing.Id;
            }
        }

        var quota = SalesQuota.Create(
            request.TenantId,
            request.UserId,
            request.PeriodType,
            request.PeriodStart,
            request.PeriodEnd,
            request.TargetAmount);

        await _repository.AddAsync(quota, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return quota.Id;
    }
}
