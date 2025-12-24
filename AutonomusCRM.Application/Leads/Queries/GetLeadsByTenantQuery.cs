using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Leads.Queries;

public record GetLeadsByTenantQuery(Guid TenantId, LeadStatus? Status = null) : IRequest<IEnumerable<LeadDto>>;

public record LeadDto(
    Guid Id,
    Guid TenantId,
    string Name,
    string? Email,
    string? Phone,
    string? Company,
    LeadStatus Status,
    LeadSource Source,
    int? Score,
    DateTime CreatedAt
);

