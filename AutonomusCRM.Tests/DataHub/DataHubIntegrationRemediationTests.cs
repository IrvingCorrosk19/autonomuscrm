using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubRemediation")]
public class DataHubIntegrationRemediationTests
{
    private readonly PostgresTestFixture _fixture;

    public DataHubIntegrationRemediationTests(PostgresTestFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task SaveTemplateAsync_InsertsNewTemplate()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var repo = CreateRepository(tenantId);
        var templateId = Guid.NewGuid();
        var template = new DataHubImportTemplate
        {
            Id = templateId,
            TenantId = tenantId,
            Name = "Remediation Insert Test",
            TargetEntity = "Lead",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await repo.SaveTemplateAsync(template);

        var loaded = await _fixture.Db!.DataHubImportTemplates
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.Id == templateId);
        Assert.NotNull(loaded);
        Assert.Equal("Remediation Insert Test", loaded!.Name);
    }

    [SkippableFact]
    public async Task TryClaimScheduledImport_AllowsOnlyOneConcurrentClaim()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var repo = CreateRepository(tenantId);
        var scheduleId = Guid.NewGuid();
        var dueAt = DateTime.UtcNow.AddMinutes(-1);

        _fixture.Db!.DataHubScheduledImports.Add(new DataHubScheduledImport
        {
            Id = scheduleId,
            TenantId = tenantId,
            CreatedByUserId = Guid.NewGuid(),
            Name = "Concurrency Test",
            Source = "HubSpot",
            SourceEntity = "Contacts",
            Frequency = "Daily",
            ImportMode = DataHubMigrationImportMode.Full.ToString(),
            LoadMode = DataHubLoadMode.InsertOnly.ToString(),
            IsEnabled = true,
            NextRunAt = dueAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        await _fixture.Db.SaveChangesAsync();

        var leaseUntil = DateTime.UtcNow.AddMinutes(5);

        var claimTasks = Enumerable.Range(0, 16).Select(async _ =>
        {
            var repo = CreateRepository(tenantId);
            var runId = Guid.NewGuid();
            return await repo.TryClaimScheduledImportAsync(scheduleId, runId, leaseUntil);
        }).ToArray();

        var results = await Task.WhenAll(claimTasks);
        var successes = results.Count(r => r != null);

        Assert.Equal(1, successes);
    }

    [SkippableFact]
    public async Task BulkInsertRowsCopyAsync_PersistsStagingRows()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var repo = CreateRepository(tenantId);

        _fixture.Db!.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "copy-test.csv",
            TargetEntity = "Lead",
            LoadMode = DataHubLoadMode.InsertOnly.ToString(),
            Status = DataHubJobStatus.Parsing.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await _fixture.Db.SaveChangesAsync();

        var rows = Enumerable.Range(1, 120).Select(i => new DataHubImportRow
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = i,
            RawData = new Dictionary<string, string?> { ["Email"] = $"user{i}@test.com" },
            Status = DataHubRowStatus.Pending.ToString()
        }).ToList();

        var inserted = await repo.BulkInsertRowsCopyAsync(rows);
        Assert.Equal(120, inserted);

        var count = await _fixture.Db.DataHubImportRows
            .IgnoreQueryFilters()
            .CountAsync(r => r.JobId == jobId);
        Assert.Equal(120, count);
    }

    private DataHubRepository CreateRepository(Guid tenantId)
    {
        var accessor = new TestTenantAccessor { TenantId = tenantId, BypassTenantFilter = false };
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_fixture.ConnectionString!)
                .Options,
            accessor);
        return new DataHubRepository(db);
    }
}
