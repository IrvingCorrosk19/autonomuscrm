using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;
using AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceSyncUnitTests
{
    [Fact]
    public void ConflictResolution_SourceWinsUpdatesExisting()
    {
        var resolver = new DbSyncConflictResolver();
        var decision = resolver.Resolve(DbSyncConflictPolicy.SourceWins, true, true, true);
        Assert.Equal("Update", decision.Action);
    }

    [Fact]
    public void ConflictResolution_CrmWinsSkips()
    {
        var resolver = new DbSyncConflictResolver();
        var decision = resolver.Resolve(DbSyncConflictPolicy.CrmWins, true, true, true);
        Assert.Equal("Skip", decision.Action);
    }

    [Fact]
    public void ConflictResolution_ManualReviewFlagsConflict()
    {
        var resolver = new DbSyncConflictResolver();
        var decision = resolver.Resolve(DbSyncConflictPolicy.ManualReview, true, true, true);
        Assert.Equal("Conflict", decision.Action);
    }

    [Fact]
    public void Safety_ReadOnlySqlGuardBlocksWrites()
    {
        Assert.Throws<InvalidOperationException>(() =>
            DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery("UPDATE customers SET name='x'"));
        Assert.Throws<InvalidOperationException>(() =>
            DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery("DELETE FROM customers"));
        Assert.Throws<InvalidOperationException>(() =>
            DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery("DROP TABLE customers"));
    }

    [Fact]
    public void Safety_ReadOnlySqlGuardAllowsSelect()
    {
        var ex = Record.Exception(() =>
            DbDiscoverySqlGuard.EnsureReadOnlyMetadataQuery("SELECT id, name FROM public.customers LIMIT 10"));
        Assert.Null(ex);
    }

    [Fact]
    public void DeltaDataset_FiltersByWatermark()
    {
        var watermark = DateTime.UtcNow.AddDays(-1);
        var rows = SyncSyntheticDatasets.DeltaDataset(watermark);
        var delta = rows.Where(r => !r.ModifiedAtUtc.HasValue || r.ModifiedAtUtc > watermark).ToList();
        Assert.Single(delta);
        Assert.Equal("delta@example.com", delta[0].Data["email"]);
    }

    [Fact]
    public void StagingRows_HaveValidEntityTypes()
    {
        foreach (var row in SyncSyntheticDatasets.SmbDataset())
            Assert.Contains(row.EntityType, new[] { BusinessEntityType.Customer, BusinessEntityType.Contact, BusinessEntityType.Sale });
    }

    [Fact]
    public void ProgressStages_Defined()
    {
        Assert.Equal("ReadingSource", DbSyncStages.ReadingSource);
        Assert.Equal("Completed", DbSyncStages.Completed);
    }

    [Fact]
    public void RabbitMqQueueName_IsConfigured()
    {
        var options = new DbSyncProcessingOptions();
        Assert.Equal("db-intelligence.sync.jobs", options.SyncQueueName);
    }

    [Fact]
    public void LargeDataset_HasExpectedVolume()
    {
        Assert.Equal(200, SyncSyntheticDatasets.LargeDataset().Count);
    }
}
