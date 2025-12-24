using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Leads.Commands;

public record QualifyLeadCommand(Guid LeadId, Guid TenantId) : IRequest<bool>;

