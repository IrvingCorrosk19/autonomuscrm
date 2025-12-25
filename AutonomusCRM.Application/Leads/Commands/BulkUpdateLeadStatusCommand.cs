using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Leads.Commands;

public record BulkUpdateLeadStatusCommand(
    List<Guid> LeadIds,
    Guid TenantId,
    LeadStatus NewStatus
) : IRequest<int>;

