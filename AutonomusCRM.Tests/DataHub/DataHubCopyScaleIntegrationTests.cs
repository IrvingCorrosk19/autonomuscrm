using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubCopyScale")]
public class DataHubCopyScaleIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public DataHubCopyScaleIntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    [Theory]
    [InlineData(1_000)]
    [InlineData(10_000)]
    [InlineData(50_000)]
    public async Task BulkInsertRowsCopyAsync_PersistsAllRows(int rowCount)
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = $"scale-{rowCount}.csv",
            TargetEntity = "Lead",
            Status = DataHubJobStatus.Parsing.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        const int batch = 5000;
        for (var offset = 0; offset < rowCount; offset += batch)
        {
            var take = Math.Min(batch, rowCount - offset);
            var rows = Enumerable.Range(offset + 1, take).Select(i => new DataHubImportRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                TenantId = tenantId,
                RowNumber = i,
                RawData = new Dictionary<string, string?> { ["Email"] = $"scale{i}@test.com" },
                Status = DataHubRowStatus.Pending.ToString()
            }).ToList();
            await repo.BulkInsertRowsCopyAsync(rows);
        }
        sw.Stop();

        var count = await db.DataHubImportRows.IgnoreQueryFilters().CountAsync(r => r.JobId == jobId);
        Assert.Equal(rowCount, count);
        Assert.True(sw.Elapsed.TotalSeconds < Math.Max(60, rowCount / 500.0),
            $"COPY {rowCount} rows took {sw.Elapsed.TotalSeconds:F1}s");
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task BulkInsertRowsCopyAsync_100K_PersistsAllRows()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);
        const int rowCount = 100_000;

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "scale-100k.csv",
            TargetEntity = "Lead",
            Status = DataHubJobStatus.Parsing.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        for (var offset = 0; offset < rowCount; offset += 10_000)
        {
            var rows = Enumerable.Range(offset + 1, 10_000).Select(i => new DataHubImportRow
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                TenantId = tenantId,
                RowNumber = i,
                RawData = new Dictionary<string, string?> { ["Email"] = $"scale{i}@test.com" },
                Status = DataHubRowStatus.Pending.ToString()
            }).ToList();
            await repo.BulkInsertRowsCopyAsync(rows);
        }

        var count = await db.DataHubImportRows.IgnoreQueryFilters().CountAsync(r => r.JobId == jobId);
        Assert.Equal(rowCount, count);
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task BulkInsertRowsCopyAsync_100K_ChunkFailure_RollsBackAllRows()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);
        const int totalRows = 100_000;

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "scale-100k-tx.csv",
            TargetEntity = "Lead",
            Status = DataHubJobStatus.Parsing.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var firstBatch = Enumerable.Range(1, 50_000).Select(i => new DataHubImportRow
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = i,
            RawData = new Dictionary<string, string?> { ["Email"] = $"tx{i}@test.com" },
            Status = DataHubRowStatus.Pending.ToString()
        }).ToList();
        var secondBatch = Enumerable.Range(50_001, 50_000).Select(i => new DataHubImportRow
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = i,
            RawData = new Dictionary<string, string?> { ["Email"] = $"tx{i}@test.com" },
            Status = DataHubRowStatus.Pending.ToString()
        }).ToList();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            repo.ExecuteInTransactionAsync(async () =>
            {
                await repo.BulkInsertRowsCopyAsync(firstBatch);
                await repo.BulkInsertRowsCopyAsync(secondBatch);
                throw new InvalidOperationException("simulated chunk-2 failure at 100K scale");
            }));

        var count = await db.DataHubImportRows.IgnoreQueryFilters().CountAsync(r => r.JobId == jobId);
        Assert.Equal(0, count);
        await db.DisposeAsync();
    }

    private (ApplicationDbContext Db, DataHubRepository Repo) CreateContext(Guid tenantId)
    {
        var accessor = new TestTenantAccessor { TenantId = tenantId, BypassTenantFilter = true };
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>().UseNpgsql(_fixture.ConnectionString!).Options,
            accessor);
        return (db, new DataHubRepository(db));
    }
}
