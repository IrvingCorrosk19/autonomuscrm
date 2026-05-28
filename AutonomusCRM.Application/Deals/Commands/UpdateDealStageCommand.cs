using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Commands;

public record UpdateDealStageCommand(
    Guid DealId,
    Guid TenantId,
    DealStage Stage,
    int? Probability = null,
    int? ExpectedVersion = null
) : IRequest<bool>;

