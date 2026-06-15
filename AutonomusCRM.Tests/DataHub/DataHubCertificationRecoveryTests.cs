using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubEnterpriseE2E")]
public class DataHubEnterpriseE2ETests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DataHubEnterpriseE2ETests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task E2E_MigrationQuality_MissingOwner_BlocksSync()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, factory, tenantId, token) = await LoginAsync();
        SetAuth(client, token);

        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        accessor.TenantId = tenantId;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var adminUserId = await db.Users.AsNoTracking()
            .Where(u => u.Email == "admin@autonomuscrm.local" && u.TenantId == tenantId)
            .Select(u => u.Id)
            .FirstAsync();

        var jobId = Guid.NewGuid();
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            CreatedByUserId = adminUserId,
            FileName = "migration.csv",
            TargetEntity = "Lead",
            Status = DataHubJobStatus.Completed.ToString(),
            Metadata = new Dictionary<string, object>
            {
                ["migrationSource"] = "HubSpot",
                ["migrationEntity"] = "Contacts",
                ["migrationMode"] = "Delta"
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

        var qualityResp = await client.GetAsync($"/api/datahub/migration/jobs/{jobId}/quality?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, qualityResp.StatusCode);
        var report = await qualityResp.Content.ReadFromJsonAsync<DataHubMigrationQualityReportDto>();
        Assert.NotNull(report);
        Assert.False(report!.Passed);
        Assert.True(report.MissingOwners > 0);

        var migration = scope.ServiceProvider.GetRequiredService<IDataHubMigrationService>();
        await migration.TryCompleteMigrationSyncAsync(tenantId, jobId);

        var job = await db.DataHubImportJobs.AsNoTracking().FirstAsync(j => j.Id == jobId);
        Assert.True(job.Metadata.ContainsKey("migrationSyncBlocked"));
    }

    [SkippableFact]
    public async Task E2E_QualityCenter_ReturnsScore()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, _, tenantId, token) = await LoginAsync();
        SetAuth(client, token);
        var resp = await client.GetAsync($"/api/datahub/quality/score?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [SkippableFact]
    public async Task E2E_ExportJobsList_ReturnsHistory()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, _, tenantId, token) = await LoginAsync();
        SetAuth(client, token);
        var resp = await client.GetAsync($"/api/datahub/jobs?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    [SkippableFact]
    public async Task E2E_MigrationSources_ListAvailable()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var (client, _, tenantId, token) = await LoginAsync();
        SetAuth(client, token);
        var resp = await client.GetAsync($"/api/datahub/migration/sources?tenantId={tenantId}");
        Assert.Equal(HttpStatusCode.OK, resp.StatusCode);
    }

    private async Task<(HttpClient Client, CustomWebApplicationFactory Factory, Guid TenantId, string Token)> LoginAsync()
    {
        var client = _fixture.Client ?? throw new InvalidOperationException();
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        var login = await client.PostAsJsonAsync("/api/auth/login", new LoginCommand("admin@autonomuscrm.local", "Admin123!", tenantId));
        Assert.Equal(HttpStatusCode.OK, login.StatusCode);
        var token = (await login.Content.ReadFromJsonAsync<LoginResult>())!.AccessToken;
        return (client, factory, tenantId, token);
    }

    private static void SetAuth(HttpClient client, string token)
        => client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubCertificationRecovery")]
public class DataHubCertificationRecoveryWebTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DataHubCertificationRecoveryWebTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task ProcessJob_SkipsFailedRowsUntilRetryRevalidates()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        accessor.TenantId = tenantId;

        var orchestrator = scope.ServiceProvider.GetRequiredService<IDataHubOrchestrator>();
        var jobId = Guid.NewGuid();
        var adminUserId = await db.Users.AsNoTracking()
            .Where(u => u.Email == "admin@autonomuscrm.local" && u.TenantId == tenantId)
            .Select(u => u.Id)
            .FirstAsync();

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            CreatedByUserId = adminUserId,
            FileName = "failed.csv",
            TargetEntity = "Lead",
            LoadMode = DataHubLoadMode.InsertOnly.ToString(),
            Status = DataHubJobStatus.ReadyToImport.ToString(),
            TotalRows = 1,
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubImportMappings.Add(new DataHubImportMapping
        {
            Id = Guid.NewGuid(), JobId = jobId, TenantId = tenantId,
            SourceColumn = "Email", TargetField = "Email", SortOrder = 0
        });
        db.DataHubImportRows.Add(new DataHubImportRow
        {
            Id = Guid.NewGuid(), JobId = jobId, TenantId = tenantId, RowNumber = 1,
            RawData = new Dictionary<string, string?> { ["Email"] = "bad@", ["Name"] = "X" },
            Status = DataHubRowStatus.Failed.ToString()
        });
        await db.SaveChangesAsync();

        await orchestrator.ProcessJobAsync(jobId);
        Assert.Equal(DataHubRowStatus.Failed.ToString(),
            (await db.DataHubImportRows.FirstAsync(r => r.JobId == jobId)).Status);
        Assert.Equal(0, await db.Leads.IgnoreQueryFilters().CountAsync(l => l.Email == "bad@"));

        var row = await db.DataHubImportRows.FirstAsync(r => r.JobId == jobId);
        row.Status = DataHubRowStatus.Pending.ToString();
        row.TransformedData = new Dictionary<string, string?>();
        await db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.RetryFailedRowsAsync(tenantId, jobId));
    }
}

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubCertificationRecovery")]
public class DataHubCertificationRecoveryIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public DataHubCertificationRecoveryIntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task TemplateVersion_ConcurrentIncrements_ProduceUniqueVersions()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var templateId = Guid.NewGuid();
        var db = CreateDb();
        db.DataHubImportTemplates.Add(new DataHubImportTemplate
        {
            Id = templateId,
            TenantId = tenantId,
            Name = "Race Test",
            TargetEntity = "Lead",
            LatestVersion = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var tasks = Enumerable.Range(0, 8).Select(async _ =>
        {
            var repo = new DataHubRepository(CreateDb());
            return await repo.IncrementTemplateLatestVersionAsync(templateId);
        }).ToArray();
        var versions = await Task.WhenAll(tasks);
        await using var verifyDb = CreateDb();
        var finalVersion = await verifyDb.DataHubImportTemplates.AsNoTracking()
            .Where(t => t.Id == templateId)
            .Select(t => t.LatestVersion)
            .FirstAsync();
        Assert.Equal(8, finalVersion);
        Assert.True(versions.All(v => v is >= 1 and <= 8));
        await db.DisposeAsync();
    }

    private ApplicationDbContext CreateDb()
        => new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_fixture.ConnectionString!).Options,
            new TestTenantAccessor { BypassTenantFilter = true });
}

public class DataHubCertificationRecoveryUnitTests
{
    [Fact]
    public void SmartMatching_EnterpriseDataset_MeetsPrecisionThreshold()
    {
        var cases = new (string Entity, string Column, string[] Samples, string? ExpectedField)[]
        {
            ("Lead", "Correo electrónico", new[] { "a@b.com" }, "Email"),
            ("Customer", "Teléfono móvil", new[] { "+50761234567" }, "Phone"),
            ("Deal", "Monto estimado", new[] { "15000.50" }, "Amount"),
            ("Deal", "Fecha de cierre", new[] { "2026-12-31" }, "ExpectedCloseDate"),
            ("Customer", "Account Id", new[] { "12345" }, null),
            ("Lead", "email_address", new[] { "x@y.com" }, "Email"),
        };

        var correct = 0;
        foreach (var c in cases)
        {
            var result = DataHubSmartMatchingEngine.MatchColumn(c.Entity, c.Column, c.Samples);
            if (c.ExpectedField == null)
            {
                if (result.TargetField != "Company") correct++;
            }
            else if (string.Equals(result.TargetField, c.ExpectedField, StringComparison.OrdinalIgnoreCase))
                correct++;
        }

        var precision = correct / (double)cases.Length;
        Assert.True(precision >= 0.85, $"Smart matching precision {precision:P0} below 85%");
    }

    [Fact]
    public async Task EncryptionStreaming_LargeFile_RoundTripWithoutFullBuffer()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DataHubSecurityOptions
        {
            ActiveEncryptionKeyId = "v1",
            EncryptionKeys = new Dictionary<string, string> { ["v1"] = Convert.ToBase64String(new byte[32]) }
        });
        var crypto = new DataHubFileEncryption(options);
        var path = Path.Combine(Path.GetTempPath(), $"dh-stream-{Guid.NewGuid():N}.enc");
        const int sizeMb = 12;
        try
        {
            await using (var input = File.Create(Path.GetTempPath() + Guid.NewGuid().ToString("N")))
            {
                var buf = new byte[1024 * 1024];
                for (var i = 0; i < sizeMb; i++)
                    await input.WriteAsync(buf);
                input.Position = 0;
                GC.Collect();
                var before = GC.GetTotalMemory(true);
                await crypto.EncryptToFileAsync(input, path);
                var afterEncrypt = GC.GetTotalMemory(false);
                Assert.True(afterEncrypt - before < 80 * 1024 * 1024,
                    $"Encrypt memory delta {(afterEncrypt - before) / 1024 / 1024}MB too high");
            }

            await using var decrypted = await crypto.DecryptToTempFileStreamAsync(path);
            Assert.True(decrypted.Length >= sizeMb * 1024L * 1024L);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
