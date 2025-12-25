using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Commands;

public record BulkUpdateDealStageCommand(
    List<Guid> DealIds,
    Guid TenantId,
    DealStage NewStage,
    int? Probability = null
) : IRequest<int>;

