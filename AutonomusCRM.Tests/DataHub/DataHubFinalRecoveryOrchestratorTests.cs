using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using AutonomusCRM.Tests.Integration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

[Collection("PostgresWebIntegration")]
[Trait("Category", "Integration")]
[Trait("Category", "DataHubFinalRecovery")]
public class DataHubFinalRecoveryOrchestratorTests
{
    private readonly PostgresWebApplicationFixture _fixture;

    public DataHubFinalRecoveryOrchestratorTests(PostgresWebApplicationFixture fixture) => _fixture = fixture;

    [SkippableFact]
    public async Task RetryFailedRows_WithInvalidEmail_RevalidatesAndBlocksImport()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        accessor.TenantId = tenantId;

        var repo = scope.ServiceProvider.GetRequiredService<IDataHubRepository>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IDataHubOrchestrator>();
        var jobId = Guid.NewGuid();

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "retry-invalid.csv",
            TargetEntity = "Lead",
            LoadMode = DataHubLoadMode.InsertOnly.ToString(),
            Status = DataHubJobStatus.CompletedWithErrors.ToString(),
            TotalRows = 1,
            DetectedColumns = new List<string> { "Email", "Name" },
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubImportMappings.Add(new DataHubImportMapping
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            SourceColumn = "Email",
            TargetField = "Email",
            IsRequired = true,
            SortOrder = 0
        });
        db.DataHubImportMappings.Add(new DataHubImportMapping
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            SourceColumn = "Name",
            TargetField = "Name",
            IsRequired = true,
            SortOrder = 1
        });
        db.DataHubImportRows.Add(new DataHubImportRow
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = 1,
            RawData = new Dictionary<string, string?> { ["Email"] = "not-an-email", ["Name"] = "Bad Row" },
            Status = DataHubRowStatus.Failed.ToString()
        });
        await db.SaveChangesAsync();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            orchestrator.RetryFailedRowsAsync(tenantId, jobId));

        Assert.Contains("Retry validation failed", ex.Message, StringComparison.OrdinalIgnoreCase);

        var job = await repo.GetJobAsync(tenantId, jobId);
        Assert.Equal(DataHubJobStatus.ValidationFailed.ToString(), job!.Status);
        Assert.False(job.SuccessRows > 0 && job.FailedRows == 0);
    }

    [SkippableFact]
    public async Task RecoverOrphanJob_WithInvalidRows_RevalidatesAndDoesNotImport()
    {
        IntegrationTestSkip.IfUnavailable(_fixture.SkipReason);
        var factory = _fixture.Factory ?? throw new InvalidOperationException();
        using var scope = factory.Services.CreateScope();
        var accessor = scope.ServiceProvider.GetRequiredService<ICurrentTenantAccessor>();
        accessor.BypassTenantFilter = true;
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var tenantId = await db.Tenants.AsNoTracking().Select(t => t.Id).FirstAsync();
        accessor.TenantId = tenantId;

        var repo = scope.ServiceProvider.GetRequiredService<IDataHubRepository>();
        var orchestrator = scope.ServiceProvider.GetRequiredService<IDataHubOrchestrator>();
        var jobId = Guid.NewGuid();

        db.DataHubImportJobs.Add(new DataHubImportJob
        {
            Id = jobId,
            TenantId = tenantId,
            FileName = "orphan-invalid.csv",
            TargetEntity = "Lead",
            LoadMode = DataHubLoadMode.InsertOnly.ToString(),
            Status = DataHubJobStatus.Importing.ToString(),
            TotalRows = 1,
            DetectedColumns = new List<string> { "Email", "Name" },
            StartedAt = DateTime.UtcNow.AddMinutes(-30),
            CreatedAt = DateTime.UtcNow
        });
        db.DataHubImportMappings.Add(new DataHubImportMapping
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            SourceColumn = "Email",
            TargetField = "Email",
            IsRequired = true,
            SortOrder = 0
        });
        db.DataHubImportRows.Add(new DataHubImportRow
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = 1,
            RawData = new Dictionary<string, string?> { ["Email"] = "bad@", ["Name"] = "Orphan" },
            Status = DataHubRowStatus.Valid.ToString()
        });
        await db.SaveChangesAsync();

        await orchestrator.RecoverOrphanJobAsync(jobId);

        var job = await repo.GetJobAsync(tenantId, jobId);
        Assert.Equal(DataHubJobStatus.ValidationFailed.ToString(), job!.Status);
        Assert.True(job.FailedRows >= 1);
        Assert.NotEqual(DataHubJobStatus.ReadyToImport.ToString(), job.Status);
        Assert.NotEqual(DataHubJobStatus.Importing.ToString(), job.Status);
    }
}
