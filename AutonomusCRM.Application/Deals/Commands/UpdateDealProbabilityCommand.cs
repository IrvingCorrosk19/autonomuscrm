using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Deals.Commands;

public record UpdateDealProbabilityCommand(
    Guid DealId,
    Guid TenantId,
    int Probability
) : IRequest<bool>;

