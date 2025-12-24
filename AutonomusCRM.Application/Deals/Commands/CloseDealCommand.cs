using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Deals.Commands;

public record CloseDealCommand(
    Guid DealId,
    Guid TenantId,
    decimal? FinalAmount = null
) : IRequest<bool>;

