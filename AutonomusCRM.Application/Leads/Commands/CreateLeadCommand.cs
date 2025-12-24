using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Leads.Commands;

public record CreateLeadCommand(
    Guid TenantId,
    string Name,
    LeadSource Source,
    string? Email = null,
    string? Phone = null,
    string? Company = null
) : IRequest<Guid>;

