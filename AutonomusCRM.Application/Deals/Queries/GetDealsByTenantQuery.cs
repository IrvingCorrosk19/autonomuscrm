using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Queries;

public record GetDealsByTenantQuery(
    Guid TenantId,
    DealStatus? Status = null,
    DealStage? Stage = null
) : IRequest<IEnumerable<DealDto>>;

public record DealDto(
    Guid Id,
    Guid TenantId,
    Guid CustomerId,
    string Title,
    decimal Amount,
    DealStatus Status,
    DealStage Stage,
    int? Probability,
    DateTime? ExpectedCloseDate,
    DateTime CreatedAt
);

