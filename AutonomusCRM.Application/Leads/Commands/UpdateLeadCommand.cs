using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Leads.Commands;

public record UpdateLeadCommand(
    Guid LeadId,
    Guid TenantId,
    string Name,
    LeadSource Source,
    string? Email = null,
    string? Phone = null,
    string? Company = null,
    LeadStatus? Status = null
) : IRequest<bool>;

