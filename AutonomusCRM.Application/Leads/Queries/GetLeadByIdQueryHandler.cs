using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Leads.Queries;

public class GetLeadByIdQueryHandler : IRequestHandler<GetLeadByIdQuery, LeadDto?>
{
    private readonly ILeadRepository _leadRepository;

    public GetLeadByIdQueryHandler(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<LeadDto?> HandleAsync(GetLeadByIdQuery request, CancellationToken cancellationToken = default)
    {
        var lead = await _leadRepository.GetByIdAsync(request.LeadId, cancellationToken);
        if (lead is null || lead.TenantId != request.TenantId)
            return null;

        return new LeadDto(
            lead.Id,
            lead.TenantId,
            lead.Name,
            lead.Email,
            lead.Phone,
            lead.Company,
            lead.Status,
            lead.Source,
            lead.Score,
            lead.CreatedAt);
    }
}
