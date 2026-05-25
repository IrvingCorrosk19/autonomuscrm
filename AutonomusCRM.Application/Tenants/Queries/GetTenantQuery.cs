using AutonomusCRM.Application.Common.Interfaces;

namespace AutonomusCRM.Application.Tenants.Queries;

public record GetTenantQuery(Guid TenantId) : IRequest<TenantDto?>;

public record TenantDto(
    Guid Id,
    string Name,
    string? Description,
    bool IsKillSwitchEnabled,
    DateTime CreatedAt);

public class GetTenantQueryHandler : IRequestHandler<GetTenantQuery, TenantDto?>
{
    private readonly ITenantRepository _tenantRepository;

    public GetTenantQueryHandler(ITenantRepository tenantRepository)
    {
        _tenantRepository = tenantRepository;
    }

    public async Task<TenantDto?> HandleAsync(GetTenantQuery request, CancellationToken cancellationToken = default)
    {
        var tenant = await _tenantRepository.GetByIdAsync(request.TenantId, cancellationToken);
        if (tenant is null)
            return null;

        return new TenantDto(
            tenant.Id,
            tenant.Name,
            tenant.Description,
            tenant.IsKillSwitchEnabled,
            tenant.CreatedAt);
    }
}
