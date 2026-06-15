using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.Events.EventBus;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubCertificationBlockers")]
public class DataHubCertificationBlockerTests
{
    private readonly PostgresWebApplicationFixture _fixture;
    private static readonly string E2eDir = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "ops", "certification", "datahub-e2e"));

    public DataHubCertificationBlockerTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [SkippableTheory]
    [InlineData("Wizard")]
    [InlineData("Scheduled")]
    [InlineData("Manual")]
    public async Task E2E_Migration_MissingOwner_BlocksSync(string path)
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        var (client, _, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var migration = scope.ServiceProvider.GetRequiredService<IDataHubMigrationService>();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;

        var adminUserId = await db.Users.AsNoTracking()
            .Where(u => u.Email == "admin@autonomuscrm.local" && u.TenantId == tenantId)
            .Select(u => u.Id).FirstAsync();

        var jobId = await SeedMigrationJobAsync(db, tenantId, adminUserId, path);

        if (path == "Wizard")
        {
            var sources = await client.GetAsync($"/api/datahub/migration/sources?tenantId={tenantId}");
            Assert.Equal(HttpStatusCode.OK, sources.StatusCode);
            var qualityResp = await client.GetAsync($"/api/datahub/migration/jobs/{jobId}/quality?tenantId={tenantId}");
            Assert.Equal(HttpStatusCode.OK, qualityResp.StatusCode);
            var report = await qualityResp.Content.ReadFromJsonAsync<DataHubMigrationQualityReportDto>();
            Assert.NotNull(report);
            Assert.False(report!.Passed);
        }
        else if (path == "Scheduled")
        {
            var scheduleId = Guid.NewGuid();
            db.DataHubScheduledImports.Add(new DataHubScheduledImport
            {
                Id = scheduleId,
                TenantId = tenantId,
                CreatedByUserId = adminUserId,
                Name = "Certification Scheduled",
                Source = "HubSpot",
                SourceEntity = "Contacts",
                Frequency = "Daily",
                ImportMode = DataHubMigrationImportMode.Full.ToString(),
                LoadMode = DataHubLoadMode.InsertOnly.ToString(),
                IsEnabled = true,
                NextRunAt = DateTime.UtcNow.AddHours(1),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }
        else
        {
            Assert.Equal("Manual", path);
        }

        await migration.TryCompleteMigrationSyncAsync(tenantId, jobId);

        var job = await db.DataHubImportJobs.IgnoreQueryFilters().AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.True(job.Metadata.ContainsKey("migrationSyncBlocked"));
        Assert.False(job.Metadata.ContainsKey("migrationSyncCompleted"));

        await migration.TryCompleteMigrationSyncAsync(tenantId, jobId);
        var afterBypass = await db.DataHubImportJobs.AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.True(afterBypass.Metadata.ContainsKey("migrationSyncBlocked"));
        Assert.False(afterBypass.Metadata.TryGetValue("migrationSyncCompleted", out var done) && done is true);
    }

    [SkippableFact]
    public async Task E2E_ImportThenRollbackViaApi_VerifiesDatabaseState()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var csvPath = Path.Combine(E2eDir, "leads-valid.csv");
        Skip.If(!File.Exists(csvPath), $"Missing {csvPath}");

        var jobId = await UploadAndImportAsync(client, tenantId, csvPath);
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var leadCountBefore = await db.Leads.IgnoreQueryFilters()
            .CountAsync(l => l.TenantId == tenantId && l.Email != null && l.Email.Contains("@"));
        Assert.True(leadCountBefore >= 1);

        var rollbackResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/rollback?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.NoContent, rollbackResp.StatusCode);

        var leadCountAfter = await db.Leads.IgnoreQueryFilters()
            .CountAsync(l => l.TenantId == tenantId && l.Email != null && l.Email.Contains("@"));
        Assert.True(leadCountAfter < leadCountBefore);

        var job = await db.DataHubImportJobs.IgnoreQueryFilters().AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.Equal(DataHubJobStatus.RolledBack.ToString(), job.Status);
        Assert.False(job.RollbackAvailable);
    }

    [SkippableFact]
    public async Task E2E_FullPipeline_AllStagesPass()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId, token) = await LoginAsAdminAsync();
        SetAuth(client, token);

        var csvPath = Path.Combine(E2eDir, "leads-valid.csv");
        Skip.If(!File.Exists(csvPath), $"Missing {csvPath}");

        var jobId = await UploadCsvAsync(client, tenantId, csvPath, "Lead");

        var analyzeResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/analyze?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, analyzeResp.StatusCode);

        var mapResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/automap?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, mapResp.StatusCode);

        var fixResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/autofix?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, fixResp.StatusCode);

        var rulesResp = await client.GetAsync($"/api/datahub/rules?tenantId={tenantId}&targetEntity=Lead");
        Assert.Equal(HttpStatusCode.OK, rulesResp.StatusCode);

        var valResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/validate?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, valResp.StatusCode);
        var validation = await valResp.Content.ReadFromJsonAsync<DataHubValidationResultDto>();
        Assert.NotNull(validation);
        Assert.True(validation!.ReadyToImport, $"Validation not ready: {validation.InvalidRows} invalid rows");

        var previewResp = await client.GetAsync($"/api/datahub/jobs/{jobId}?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, previewResp.StatusCode);

        var importResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/import?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, importResp.StatusCode);
        Assert.True(await WaitForJobStatusAsync(client, tenantId, jobId, "Completed", "CompletedWithErrors"));

        var qualityResp = await client.GetAsync($"/api/datahub/quality/score?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, qualityResp.StatusCode);

        var exportResp = await client.GetAsync($"/api/datahub/export?tenantId={tenantId}&entityType=Lead&format=csv");
        Assert.Equal(HttpStatusCode.OK, exportResp.StatusCode);
        Assert.True(exportResp.Content.Headers.ContentLength is null or > 0);

        var templateResp = await client.PostAsync(
            $"/api/datahub/jobs/{jobId}/templates?tenantId={tenantId}&name=Cert-{Guid.NewGuid():N}", null);
        Assert.Equal(HttpStatusCode.OK, templateResp.StatusCode);

        var migrationSources = await client.GetAsync($"/api/datahub/migration/sources?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, migrationSources.StatusCode);

        var schedules = await client.GetAsync($"/api/datahub/schedules?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, schedules.StatusCode);

        var rollbackResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/rollback?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.NoContent, rollbackResp.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var job = await db.DataHubImportJobs.IgnoreQueryFilters().AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.Equal(DataHubJobStatus.RolledBack.ToString(), job.Status);
    }

    [Fact]
    public async Task Dispatcher_RabbitMode_ThrowsWhenBrokerUnavailable()
    {
        var dispatcher = new DataHubImportDispatcher(
            new DataHubJobQueue(),
            Options.Create(new DataHubProcessingOptions
            {
                ProcessingMode = DataHubProcessingMode.RabbitMQ,
                ImportQueueName = "datahub.cert.unavailable"
            }),
            Options.Create(new RabbitMQOptions { HostName = "", Port = 5672 }),
            NullLogger<DataHubImportDispatcher>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            dispatcher.EnqueueImportJobAsync(Guid.NewGuid(), Guid.NewGuid()));
    }

    private static async Task<Guid> SeedMigrationJobAsync(
        ApplicationDbContext db, Guid tenantId, Guid userId, string path)
    {
        var jobId = Guid.NewGuid();
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            CreatedByUserId = userId,
            FileName = $"{path.ToLowerInvariant()}-migration.csv",
            TargetEntity = "Lead",
            Status = DataHubJobStatus.Completed.ToString(),
            TotalRows = 1,
            Metadata = new Dictionary<string, object>
            {
                ["migrationSource"] = "HubSpot",
                ["migrationEntity"] = "Contacts",
                ["migrationMode"] = "Full",
                ["migrationPath"] = path
            },
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubImportErrors.Add(new DataHubImportError
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = 1,
            ErrorCode = "MissingOwner",
            FieldName = "OwnerId",
            Message = "Owner not found"
        });
        await db.SaveChangesAsync();
        return jobId;
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, string Token)> LoginAsAdminAsync()
        => await IntegrationTestTenantHelper.LoginAdminAsync(
            _fixture.Client ?? throw new InvalidOperationException(),
            _fixture.Factory ?? throw new InvalidOperationException());

    private async Task<(HttpClient Client, Guid TenantId, string Token)> LoginAsAdminClientOnlyAsync()
    {
        var (client, _, tenantId, token) = await LoginAsAdminAsync();
        return (client, tenantId, token);
    }

    private static void SetAuth(HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

    private static async Task<Guid> UploadCsvAsync(HttpClient client, Guid tenantId, string path, string entity)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new ByteArrayContent(await File.ReadAllBytesAsync(path)), "file", Path.GetFileName(path));
        var resp = await client.PostAsync(
            $"/api/datahub/upload?tenantId={tenantId}&targetEntity={entity}&loadMode=InsertOnly", content);
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
        return (await resp.Content.ReadFromJsonAsync<DataHubUploadResultDto>())!.JobId;
    }

    private static async Task<Guid> UploadAndImportAsync(HttpClient client, Guid tenantId, string path)
    {
        var jobId = await UploadCsvAsync(client, tenantId, path, "Lead");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/datahub/jobs/{jobId}/analyze?tenantId={tenantId}", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/datahub/jobs/{jobId}/autofix?tenantId={tenantId}", null)).StatusCode);
        var valResp = await client.PostAsync($"/api/datahub/jobs/{jobId}/validate?tenantId={tenantId}", null);
        Assert.Equal(HttpStatusCode.OK, valResp.StatusCode);
        var validation = await valResp.Content.ReadFromJsonAsync<DataHubValidationResultDto>();
        Assert.True(validation?.ReadyToImport == true, "Import prerequisites failed validation");
        Assert.Equal(HttpStatusCode.OK, (await client.PostAsync($"/api/datahub/jobs/{jobId}/import?tenantId={tenantId}", null)).StatusCode);
        Assert.True(await WaitForJobStatusAsync(client, tenantId, jobId, "Completed", "CompletedWithErrors"));
        return jobId;
    }

    private static async Task<bool> WaitForJobStatusAsync(HttpClient client, Guid tenantId, Guid jobId, params string[] statuses)
    {
        var deadline = DateTime.UtcNow.AddSeconds(90);
        while (DateTime.UtcNow < deadline)
        {
            var resp = await client.GetAsync($"/api/datahub/jobs/{jobId}?tenantId={tenantId}");
            if (resp.IsSuccessStatusCode)
            {
                var detail = await resp.Content.ReadFromJsonAsync<DataHubJobDetailDto>();
                if (detail != null && statuses.Contains(detail.Summary.Status))
                    return true;
            }
            await Task.Delay(2000);
        }
        return false;
    }
}
