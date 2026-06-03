namespace AutonomusCRM.Application.Common.Imports;

public interface ICrmImportService
{
    Task<ImportResultDto> ImportCustomersAsync(Guid tenantId, IReadOnlyList<CustomerImportRow> rows, CancellationToken cancellationToken = default);
    Task<ImportResultDto> ImportLeadsAsync(Guid tenantId, IReadOnlyList<LeadImportRow> rows, CancellationToken cancellationToken = default);
    Task<ImportResultDto> ImportDealsAsync(Guid tenantId, IReadOnlyList<DealImportRow> rows, CancellationToken cancellationToken = default);
}
