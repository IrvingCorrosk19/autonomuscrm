using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Deals.Commands;

public record DeleteDealCommand(
    Guid DealId,
    Guid TenantId
) : IRequest<bool>;

