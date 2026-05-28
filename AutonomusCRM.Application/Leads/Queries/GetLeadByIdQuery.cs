using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Leads.Queries;

public record GetLeadByIdQuery(Guid LeadId, Guid TenantId) : IRequest<LeadDto?>;
