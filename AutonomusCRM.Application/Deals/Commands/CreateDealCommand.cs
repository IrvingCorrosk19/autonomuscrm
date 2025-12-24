using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Deals.Commands;

public record CreateDealCommand(
    Guid TenantId,
    Guid CustomerId,
    string Title,
    decimal Amount,
    string? Description = null
) : IRequest<Guid>;

