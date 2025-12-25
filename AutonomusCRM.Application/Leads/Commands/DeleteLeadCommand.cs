using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Leads.Commands;

public record DeleteLeadCommand(
    Guid LeadId,
    Guid TenantId
) : IRequest<bool>;

