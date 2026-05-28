using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Deals;

namespace AutonomusCRM.Application.Deals.Queries;

public record GetDealByIdQuery(Guid DealId, Guid TenantId) : IRequest<DealDto?>;
