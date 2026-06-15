using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Domain.Users;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.DataHub.Migration;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubFinalRecovery")]
public class DataHubFinalRecoveryIntegrationTests
{
    private readonly PostgresTestFixture _fixture;

    public DataHubFinalRecoveryIntegrationTests(PostgresTestFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task JobProcessingLock_PostgresAdvisory_AllowsOnlyOneHolder()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var jobId = Guid.NewGuid();
        var repoA = CreateRepository(Guid.NewGuid());
        var repoB = CreateRepository(Guid.NewGuid());

        Assert.True(await repoA.TryAcquireJobProcessingLockAsync(jobId));
        Assert.False(await repoB.TryAcquireJobProcessingLockAsync(jobId));

        await repoA.ReleaseJobProcessingLockAsync(jobId);
        Assert.True(await repoB.TryAcquireJobProcessingLockAsync(jobId));
        await repoB.ReleaseJobProcessingLockAsync(jobId);
    }

    [SkippableFact]
    public async Task CopyStaging_TransactionRollback_LeavesZeroRows()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "tx-test.csv",
            TargetEntity = "Lead",
            LoadMode = DataHubLoadMode.InsertOnly.ToString(),
            Status = DataHubJobStatus.Parsing.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var rows = Enumerable.Range(1, 120).Select(i => new DataHubImportRow
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
                await repo.BulkInsertRowsCopyAsync(rows.Take(60).ToList());
                throw new InvalidOperationException("simulated chunk-2 failure");
            }));

        var count = await db.DataHubImportRows
            .IgnoreQueryFilters()
            .CountAsync(r => r.JobId == jobId);
        Assert.Equal(0, count);
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task MigrationSyncCompleter_BlocksWhenMissingOwners()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
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

        var completer = new MigrationSyncCompleter(
            repo,
            new NoOpIntegrationRepository(),
            new DataHubDuplicateEngine(repo, db),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MigrationSyncCompleter>.Instance);

        await completer.TryCompleteMigrationSyncAsync(tenantId, jobId);

        var job = await db.DataHubImportJobs.AsNoTracking().IgnoreQueryFilters().FirstAsync(j => j.Id == jobId);
        Assert.True(job.Metadata.ContainsKey("migrationSyncBlocked"));
        Assert.False(job.Metadata.ContainsKey("migrationSyncCompleted"));
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task RollbackCustomerCreated_DeletesEntityFromDatabase()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);

        var customer = Customer.Create(tenantId, "Rollback Test", "rollback@test.com");
        db.Customers.Add(customer);
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "rb.csv",
            TargetEntity = "Customer",
            Status = DataHubJobStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubRollbackSnapshots.Add(new DataHubRollbackSnapshot
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = 1,
            EntityType = "Customer",
            EntityId = customer.Id,
            Action = "Created",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var rollback = new DataHubRollbackService(db, repo);
        var result = await rollback.ExecuteRollbackAsync(tenantId, jobId);

        Assert.Equal(1, result.EntitiesDeleted);
        Assert.False(await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Id == customer.Id));
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task RollbackLeadUpdated_RestoresPreviousState()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);

        var lead = Lead.Create(tenantId, "Original", LeadSource.Website, "old@lead.com", "111", "OldCo");
        db.Leads.Add(lead);
        await db.SaveChangesAsync();

        lead.UpdateInfo("Changed", "new@lead.com", "222", "NewCo", LeadSource.Referral);
        await db.SaveChangesAsync();

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "lead.csv",
            TargetEntity = "Lead",
            Status = DataHubJobStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubRollbackSnapshots.Add(new DataHubRollbackSnapshot
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = 1,
            EntityType = "Lead",
            EntityId = lead.Id,
            Action = "Updated",
            PreviousState = new Dictionary<string, object?>
            {
                ["Name"] = "Original",
                ["Email"] = "old@lead.com",
                ["Phone"] = "111",
                ["Company"] = "OldCo",
                ["Source"] = LeadSource.Website.ToString()
            },
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var rollback = new DataHubRollbackService(db, repo);
        var result = await rollback.ExecuteRollbackAsync(tenantId, jobId);

        Assert.Equal(1, result.EntitiesRestored);
        var restored = await db.Leads.IgnoreQueryFilters().FirstAsync(l => l.Id == lead.Id);
        Assert.Equal("Original", restored.Name);
        Assert.Equal("old@lead.com", restored.Email);
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task RollbackUserCreated_DeletesUser()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);

        var user = User.Create(tenantId, "rollback-user@test.com", BCrypt.Net.BCrypt.HashPassword("x"), "Rollback", "User");
        db.Users.Add(user);
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "users.csv",
            TargetEntity = "User",
            Status = DataHubJobStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubRollbackSnapshots.Add(new DataHubRollbackSnapshot
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = 1,
            EntityType = "User",
            EntityId = user.Id,
            Action = "Created",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var rollback = new DataHubRollbackService(db, repo);
        var result = await rollback.ExecuteRollbackAsync(tenantId, jobId);

        Assert.Equal(1, result.EntitiesDeleted);
        Assert.False(await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == user.Id));
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task RollbackDealCreated_DeletesDealFromDatabase()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);

        var customer = Customer.Create(tenantId, "Deal Parent", "deal-parent@test.com");
        var deal = AutonomusCRM.Domain.Deals.Deal.Create(tenantId, customer.Id, "Rollback Deal", 5000m);
        db.Customers.Add(customer);
        db.Deals.Add(deal);
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "deals.csv",
            TargetEntity = "Deal",
            Status = DataHubJobStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubRollbackSnapshots.Add(new DataHubRollbackSnapshot
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = 1,
            EntityType = "Deal",
            EntityId = deal.Id,
            Action = "Created",
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var rollback = new DataHubRollbackService(db, repo);
        var result = await rollback.ExecuteRollbackAsync(tenantId, jobId);

        Assert.Equal(1, result.EntitiesDeleted);
        Assert.False(await db.Deals.IgnoreQueryFilters().AnyAsync(d => d.Id == deal.Id));
        await db.DisposeAsync();
    }

    [SkippableFact]
    public async Task RollbackPartialBatch_OnlyRevertsRequestedBatch()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var tenantId = Guid.NewGuid();
        var jobId = Guid.NewGuid();
        var (db, repo) = CreateContext(tenantId);

        var keep = Customer.Create(tenantId, "Keep Me", "keep@test.com");
        var remove = Customer.Create(tenantId, "Remove Me", "remove@test.com");
        db.Customers.AddRange(keep, remove);
        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "partial.csv",
            TargetEntity = "Customer",
            Status = DataHubJobStatus.Completed.ToString(),
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubRollbackSnapshots.AddRange(
            new DataHubRollbackSnapshot
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                TenantId = tenantId,
                RowNumber = 1,
                BatchNumber = 1,
                EntityType = "Customer",
                EntityId = remove.Id,
                Action = "Created",
                CreatedAt = DateTime.UtcNow
            },
            new DataHubRollbackSnapshot
            {
                Id = Guid.NewGuid(),
                JobId = jobId,
                TenantId = tenantId,
                RowNumber = 2,
                BatchNumber = 2,
                EntityType = "Customer",
                EntityId = keep.Id,
                Action = "Created",
                CreatedAt = DateTime.UtcNow
            });
        await db.SaveChangesAsync();

        var rollback = new DataHubRollbackService(db, repo);
        var result = await rollback.ExecuteRollbackAsync(tenantId, jobId, batchNumber: 1);

        Assert.Equal(1, result.EntitiesDeleted);
        Assert.False(await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Id == remove.Id));
        Assert.True(await db.Customers.IgnoreQueryFilters().AnyAsync(c => c.Id == keep.Id));
        await db.DisposeAsync();
    }

    private (ApplicationDbContext Db, DataHubRepository Repo) CreateContext(Guid tenantId)
    {
        var accessor = new TestTenantAccessor { TenantId = tenantId, BypassTenantFilter = true };
        var db = new ApplicationDbContext(
            new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(_fixture.ConnectionString!)
                .Options,
            accessor);
        return (db, new DataHubRepository(db));
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

    private sealed class NoOpIntegrationRepository : ITenantIntegrationRepository
    {
        public Task<TenantIntegrationConnection?> GetAsync(Guid tenantId, string provider, CancellationToken cancellationToken = default)
            => Task.FromResult<TenantIntegrationConnection?>(null);

        public Task<IReadOnlyList<TenantIntegrationConnection>> ListAsync(Guid tenantId, CancellationToken cancellationToken = default)
            => Task.FromResult<IReadOnlyList<TenantIntegrationConnection>>(Array.Empty<TenantIntegrationConnection>());

        public Task UpsertAsync(TenantIntegrationConnection connection, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
