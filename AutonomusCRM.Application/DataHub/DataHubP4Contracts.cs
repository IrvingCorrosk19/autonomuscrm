namespace AutonomusCRM.Application.DataHub;

public record DataHubScheduledImportCreateDto(
    string Name,
    string Source,
    string SourceEntity,
    string Frequency,
    string ImportMode,
    string LoadMode = "Upsert",
    DateTime? RunOnceAt = null);

public record DataHubScheduledImportUpdateDto(
    string? Name = null,
    bool? IsEnabled = null,
    string? Frequency = null,
    string? ImportMode = null,
    string? LoadMode = null,
    DateTime? RunOnceAt = null);

public interface IDataHubScheduledImportService
{
    Task<IReadOnlyList<DataHubScheduledImportDto>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default);
    Task<DataHubScheduledImportDto> CreateAsync(Guid tenantId, Guid userId, DataHubScheduledImportCreateDto dto, CancellationToken cancellationToken = default);
    Task<DataHubScheduledImportDto?> UpdateAsync(Guid tenantId, Guid scheduleId, DataHubScheduledImportUpdateDto dto, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid tenantId, Guid scheduleId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<DataHubScheduledImportRunDto>> ListRunsAsync(Guid tenantId, Guid scheduleId, int take = 20, CancellationToken cancellationToken = default);
    Task ExecuteNowAsync(Guid tenantId, Guid userId, Guid scheduleId, CancellationToken cancellationToken = default);
    Task ProcessDueSchedulesAsync(CancellationToken cancellationToken = default);
}

public interface IDataHubTemplateVersionService
{
    Task<IReadOnlyList<DataHubTemplateVersionDto>> ListVersionsAsync(Guid tenantId, Guid templateId, CancellationToken cancellationToken = default);
    Task<DataHubTemplateVersionDto> CreateVersionAsync(Guid tenantId, Guid userId, Guid templateId, string? changeSummary, CancellationToken cancellationToken = default);
    Task<DataHubTemplateVersionCompareDto> CompareVersionsAsync(Guid tenantId, Guid templateId, int versionA, int versionB, CancellationToken cancellationToken = default);
    Task<DataHubTemplateVersionDto> RestoreVersionAsync(Guid tenantId, Guid userId, Guid templateId, int versionNumber, CancellationToken cancellationToken = default);
    Task<DataHubTemplateVersionDto> ActivateVersionAsync(Guid tenantId, Guid userId, Guid templateId, int versionNumber, CancellationToken cancellationToken = default);
    Task EnsureInitialVersionAsync(Guid tenantId, Guid userId, DataHubImportTemplate template, CancellationToken cancellationToken = default);
}
