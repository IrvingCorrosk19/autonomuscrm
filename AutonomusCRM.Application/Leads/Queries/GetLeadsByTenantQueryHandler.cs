using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Domain.Leads;

namespace AutonomusCRM.Application.Leads.Queries;

public class GetLeadsByTenantQueryHandler : IRequestHandler<GetLeadsByTenantQuery, IEnumerable<LeadDto>>
{
    private readonly ILeadRepository _leadRepository;

    public GetLeadsByTenantQueryHandler(ILeadRepository leadRepository)
    {
        _leadRepository = leadRepository;
    }

    public async Task<IEnumerable<LeadDto>> HandleAsync(GetLeadsByTenantQuery request, CancellationToken cancellationToken = default)
    {
        IEnumerable<Lead> leads;

        if (request.Status.HasValue)
        {
            leads = await _leadRepository.GetByStatusAsync(request.TenantId, request.Status.Value, cancellationToken);
        }
        else
        {
            leads = await _leadRepository.GetByTenantIdAsync(request.TenantId, cancellationToken);
        }

        return leads.Select(lead => new LeadDto(
            lead.Id,
            lead.TenantId,
            lead.Name,
            lead.Email,
            lead.Phone,
            lead.Company,
            lead.Status,
            lead.Source,
            lead.Score,
            lead.CreatedAt
        ));
    }
}

