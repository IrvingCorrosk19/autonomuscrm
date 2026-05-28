using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Deals.Commands;

public record LoseDealCommand(Guid DealId, Guid TenantId, string? Reason, string? LossCategory = null) : IRequest<bool>;
