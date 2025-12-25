using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Commands;

public record UpdateDealCommand(
    Guid DealId,
    Guid TenantId,
    string Title,
    string? Description = null,
    decimal? Amount = null,
    Guid? CustomerId = null,
    DealStage? Stage = null,
    int? Probability = null,
    DateTime? ExpectedCloseDate = null
) : IRequest<bool>;

