using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Sync;

public sealed class DbSyncConflictResolver : IDbSyncConflictResolver
{
    public DbSyncConflictDecision Resolve(string conflictPolicy, bool existsInCrm, bool sourceIsNewer, bool hasConflict)
    {
        if (!hasConflict && !existsInCrm)
            return new DbSyncConflictDecision("Create", "New record");

        if (!hasConflict && existsInCrm)
            return new DbSyncConflictDecision("Skip", "Already exists");

        return conflictPolicy switch
        {
            DbSyncConflictPolicy.SourceWins => new DbSyncConflictDecision(
                existsInCrm ? "Update" : "Create", "Source wins policy"),
            DbSyncConflictPolicy.CrmWins => new DbSyncConflictDecision(
                "Skip", "CRM wins policy"),
            DbSyncConflictPolicy.ManualReview => new DbSyncConflictDecision(
                "Conflict", "Manual review required"),
            _ => new DbSyncConflictDecision("Skip", "Unknown policy")
        };
    }
}
