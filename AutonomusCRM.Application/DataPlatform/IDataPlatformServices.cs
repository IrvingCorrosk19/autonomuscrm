namespace AutonomusCRM.Application.DataPlatform;

public record Customer360Dto(
    Guid CustomerId,
    string Name,
    string? Email,
    decimal OpenPipeline,
    decimal WonRevenue,
    int UsageEvents30d,
    double? ChurnRisk,
    IReadOnlyList<string> RecentActions);

public record DataIngestResultDto(int Accepted, int Deduplicated, int Normalized);

public interface ICustomer360Service
{
    Task<Customer360Dto?> GetAsync(Guid tenantId, Guid customerId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Customer360Dto>> SearchAsync(Guid tenantId, string? query, int take = 20, CancellationToken cancellationToken = default);
}

public interface IDataAcquisitionService
{
    Task<DataIngestResultDto> IngestWebhookBatchAsync(Guid tenantId, string entityType, IReadOnlyList<Dictionary<string, object?>> records, CancellationToken cancellationToken = default);
}

public interface IMarketplaceCatalogService
{
    IReadOnlyList<MarketplaceExtensionDto> ListExtensions();
}

public record MarketplaceExtensionDto(string Id, string Name, string Version, string[] Scopes, string Status);

public record IdentityDuplicateGroupDto(string NormalizedEmail, Guid CanonicalCustomerId, IReadOnlyList<Guid> DuplicateCustomerIds);

public interface IIdentityResolutionService
{
    Task<IReadOnlyList<IdentityDuplicateGroupDto>> FindDuplicatesByEmailAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<Guid?> ResolveCanonicalCustomerIdAsync(Guid tenantId, string? email, CancellationToken cancellationToken = default);
}
