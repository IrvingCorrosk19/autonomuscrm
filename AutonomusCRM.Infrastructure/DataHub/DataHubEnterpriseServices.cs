using System.Text.Json;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Domain.Customers;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubRollbackService : IDataHubRollbackService
{
    private readonly ApplicationDbContext _db;
    private readonly IDataHubRepository _repo;

    public DataHubRollbackService(ApplicationDbContext db, IDataHubRepository repo)
    {
        _db = db;
        _repo = repo;
    }

    public DataHubRollbackSnapshot CreateSnapshot(
        Guid jobId, Guid tenantId, int rowNumber, int? batchNumber,
        string entityType, Guid entityId, string action,
        Dictionary<string, object?>? previousState = null)
        => new()
        {
            Id = Guid.NewGuid(),
            JobId = jobId,
            TenantId = tenantId,
            RowNumber = rowNumber,
            BatchNumber = batchNumber,
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            PreviousState = previousState ?? new Dictionary<string, object?>()
        };

    public async Task<DataHubRollbackResultDto> ExecuteRollbackAsync(
        Guid tenantId, Guid jobId, int? batchNumber = null, int? rowNumber = null,
        CancellationToken cancellationToken = default)
    {
        var job = await _repo.GetJobAsync(tenantId, jobId, cancellationToken)
            ?? throw new InvalidOperationException("Job not found");

        var snapshots = (await _repo.GetRollbackSnapshotsAsync(tenantId, jobId, cancellationToken))
            .Where(s => !s.RolledBack)
            .ToList();

        if (rowNumber.HasValue)
        {
            var rows = await _repo.GetRowsAsync(tenantId, jobId, 0, job.TotalRows, cancellationToken);
            var row = rows.FirstOrDefault(r => r.RowNumber == rowNumber.Value);
            snapshots = snapshots
                .Where(s => s.RowNumber == rowNumber || (row?.CreatedEntityId != null && s.EntityId == row.CreatedEntityId))
                .ToList();
        }
        else if (batchNumber.HasValue)
        {
            snapshots = snapshots.Where(s => s.BatchNumber == batchNumber).ToList();
        }

        var deleted = 0;
        var restored = 0;
        await _repo.ExecuteInTransactionAsync(async () =>
        {
            foreach (var snap in snapshots.OrderByDescending(s => s.CreatedAt))
            {
                if (snap.Action == "Created")
                {
                    if (await DeleteEntityAsync(tenantId, snap.EntityType, snap.EntityId, cancellationToken))
                        deleted++;
                }
                else if (snap.Action == "Updated")
                {
                    if (await RestoreEntityAsync(tenantId, snap, cancellationToken))
                        restored++;
                }

                snap.RolledBack = true;
                _db.DataHubRollbackSnapshots.Update(snap);
            }

            var affectedRows = await _repo.GetRowsAsync(tenantId, jobId, 0, job.TotalRows, cancellationToken);
            foreach (var row in affectedRows)
            {
                if (!rowNumber.HasValue && batchNumber.HasValue && row.BatchNumber != batchNumber) continue;
                if (rowNumber.HasValue && row.RowNumber != rowNumber) continue;
                if (row.Status == DataHubRowStatus.Imported.ToString())
                    row.Status = DataHubRowStatus.RolledBack.ToString();
            }
            await _repo.UpdateRowsAsync(affectedRows, cancellationToken);

            var isFullRollback = !batchNumber.HasValue && !rowNumber.HasValue;
            job.Status = isFullRollback
                ? DataHubJobStatus.RolledBack.ToString()
                : job.Status;
            var remaining = await _repo.GetRollbackSnapshotsAsync(tenantId, jobId, cancellationToken);
            job.RollbackAvailable = remaining.Any(s => !s.RolledBack);
            job.Metadata = new Dictionary<string, object>(job.Metadata)
            {
                ["lastRollback"] = DateTime.UtcNow
            };
            await _repo.UpdateJobAsync(job, cancellationToken);

            await _repo.AddLogAsync(new DataHubImportLog
            {
                Id = Guid.NewGuid(), JobId = jobId, TenantId = tenantId,
                Level = "Warning",
                Message = $"Rollback executed: deleted={deleted}, restored={restored}, scope={ScopeLabel(batchNumber, rowNumber)}"
            }, cancellationToken);

            await _db.SaveChangesAsync(cancellationToken);
        }, cancellationToken);

        return new DataHubRollbackResultDto(jobId, deleted, restored, snapshots.Count, ScopeLabel(batchNumber, rowNumber));
    }

    private static string ScopeLabel(int? batch, int? row)
        => row.HasValue ? $"row:{row}" : batch.HasValue ? $"batch:{batch}" : "full";

    private async Task<bool> DeleteEntityAsync(Guid tenantId, string entityType, Guid entityId, CancellationToken ct)
    {
        return entityType switch
        {
            "Customer" => await DeleteIfExists(_db.Customers, tenantId, entityId, ct),
            "Lead" => await DeleteIfExists(_db.Leads, tenantId, entityId, ct),
            "Deal" => await DeleteIfExists(_db.Deals, tenantId, entityId, ct),
            "User" => await DeleteIfExists(_db.Users, tenantId, entityId, ct),
            _ => false
        };
    }

    private static async Task<bool> DeleteIfExists<T>(DbSet<T> set, Guid tenantId, Guid id, CancellationToken ct)
        where T : class
    {
        var entity = await set.FindAsync([id], ct);
        if (entity == null) return false;
        var tidProp = entity.GetType().GetProperty("TenantId");
        if (tidProp?.GetValue(entity) is Guid tid && tid != tenantId) return false;
        set.Remove(entity);
        return true;
    }

    private async Task<bool> RestoreEntityAsync(Guid tenantId, DataHubRollbackSnapshot snap, CancellationToken ct)
    {
        var state = snap.PreviousState;
        return snap.EntityType switch
        {
            "Customer" => await RestoreCustomer(tenantId, snap.EntityId, state, ct),
            "Lead" => await RestoreLead(tenantId, snap.EntityId, state, ct),
            "Deal" => await RestoreDeal(tenantId, snap.EntityId, state, ct),
            "User" => await RestoreUser(tenantId, snap.EntityId, state, ct),
            _ => false
        };
    }

    private async Task<bool> RestoreUser(Guid tenantId, Guid id, Dictionary<string, object?> state, CancellationToken ct)
    {
        var u = await _db.Users.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (u == null) return false;
        u.UpdateInfo(
            GetString(state, "FirstName") ?? u.FirstName,
            GetString(state, "LastName") ?? u.LastName,
            GetString(state, "Email"));
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> RestoreDeal(Guid tenantId, Guid id, Dictionary<string, object?> state, CancellationToken ct)
    {
        var d = await _db.Deals.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (d == null) return false;
        var title = GetString(state, "Title") ?? d.Title;
        decimal? amount = null;
        if (state.TryGetValue("Amount", out var amt) && amt != null &&
            decimal.TryParse(amt.ToString(), out var parsed))
            amount = parsed;
        d.UpdateInfo(title, d.Description, amount);
        await _db.SaveChangesAsync(ct);
        return true;
    }

    private async Task<bool> RestoreCustomer(Guid tenantId, Guid id, Dictionary<string, object?> state, CancellationToken ct)
    {
        var c = await _db.Customers.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (c == null) return false;
        c.UpdateInfo(
            GetString(state, "Name") ?? c.Name,
            GetString(state, "Email"),
            GetString(state, "Phone"),
            GetString(state, "Company"));
        return true;
    }

    private async Task<bool> RestoreLead(Guid tenantId, Guid id, Dictionary<string, object?> state, CancellationToken ct)
    {
        var l = await _db.Leads.FirstOrDefaultAsync(x => x.Id == id && x.TenantId == tenantId, ct);
        if (l == null) return false;
        var source = Enum.TryParse<LeadSource>(GetString(state, "Source") ?? "Other", true, out var s) ? s : LeadSource.Other;
        l.UpdateInfo(
            GetString(state, "Name") ?? l.Name,
            GetString(state, "Email"),
            GetString(state, "Phone"),
            GetString(state, "Company"),
            source);
        return true;
    }

    private static string? GetString(Dictionary<string, object?> state, string key)
    {
        if (!state.TryGetValue(key, out var v) || v == null) return null;
        if (v is JsonElement je) return je.ToString();
        return v.ToString();
    }

    public static Dictionary<string, object?> CaptureCustomerState(Customer c)
        => new()
        {
            ["Name"] = c.Name,
            ["Email"] = c.Email,
            ["Phone"] = c.Phone,
            ["Company"] = c.Company
        };

    public static Dictionary<string, object?> CaptureLeadState(Lead l)
        => new()
        {
            ["Name"] = l.Name,
            ["Email"] = l.Email,
            ["Phone"] = l.Phone,
            ["Company"] = l.Company,
            ["Source"] = l.Source.ToString()
        };

    public static Dictionary<string, object?> CaptureDealState(Deal d)
        => new()
        {
            ["Title"] = d.Title,
            ["Amount"] = d.Amount
        };

    public static Dictionary<string, object?> CaptureUserState(Domain.Users.User u)
        => new()
        {
            ["Email"] = u.Email,
            ["FirstName"] = u.FirstName,
            ["LastName"] = u.LastName
        };
}

public sealed class DataHubDuplicateEngine : IDataHubDuplicateEngine
{
    private readonly IDataHubRepository _repo;
    private readonly ApplicationDbContext _db;

    public DataHubDuplicateEngine(IDataHubRepository repo, ApplicationDbContext db)
    {
        _repo = repo;
        _db = db;
    }

    public IReadOnlyList<DataHubDuplicateMatchField> GetActiveMatchFields(string targetEntity)
        => targetEntity switch
        {
            "Lead" or "Customer" => [DataHubDuplicateMatchField.Email, DataHubDuplicateMatchField.Phone, DataHubDuplicateMatchField.Company, DataHubDuplicateMatchField.NameAndCompany],
            _ => [DataHubDuplicateMatchField.Email]
        };

    public async Task<DataHubDuplicateScanResultDto> ScanJobAsync(Guid tenantId, Guid jobId, CancellationToken cancellationToken = default)
    {
        var job = await _repo.GetJobAsync(tenantId, jobId, cancellationToken)
            ?? throw new InvalidOperationException("Job not found");
        var rows = await _repo.GetRowsAsync(tenantId, jobId, 0, job.TotalRows, cancellationToken);
        var groups = new List<DataHubDuplicateGroupDto>();

        foreach (var field in GetActiveMatchFields(job.TargetEntity))
        {
            groups.AddRange(await FindGroupsAsync(tenantId, job.TargetEntity, field, rows, cancellationToken));
        }

        var distinct = groups
            .GroupBy(g => g.MatchKey + "|" + g.MatchField)
            .Select(g => g.First())
            .ToList();

        return new DataHubDuplicateScanResultDto(
            jobId,
            distinct.Count,
            distinct.Sum(g => g.RowNumbers.Count),
            distinct);
    }

    public async Task<int> ApplyDuplicatePolicyAsync(Guid tenantId, Guid jobId, string targetEntity, string loadMode, CancellationToken cancellationToken = default)
    {
        var scan = await ScanJobAsync(tenantId, jobId, cancellationToken);
        if (scan.TotalGroups == 0) return 0;

        var job = await _repo.GetJobAsync(tenantId, jobId, cancellationToken)!;
        var rows = await _repo.GetRowsAsync(tenantId, jobId, 0, job!.TotalRows, cancellationToken);
        var marked = 0;

        foreach (var group in scan.Groups)
        {
            var action = ResolveAction(loadMode, group.ExistingEntityId.HasValue);
            var keep = group.PrimaryRowNumber;
            foreach (var rowNum in group.RowNumbers.Where(n => n != keep))
            {
                var row = rows.FirstOrDefault(r => r.RowNumber == rowNum);
                if (row == null) continue;

                if (action == DataHubDuplicateAction.Skip)
                {
                    row.Status = DataHubRowStatus.Skipped.ToString();
                    row.TransformedData["_duplicateReason"] = $"Duplicate {group.MatchField}: {group.MatchKey}";
                    marked++;
                }
                else if (action == DataHubDuplicateAction.Update && group.ExistingEntityId.HasValue)
                {
                    row.TransformedData["_upsertTargetId"] = group.ExistingEntityId.Value.ToString();
                }
            }
        }

        await _repo.UpdateRowsAsync(rows, cancellationToken);
        return marked;
    }

    private static DataHubDuplicateAction ResolveAction(string loadMode, bool existsInCrm)
        => loadMode switch
        {
            nameof(DataHubLoadMode.SkipDuplicates) => DataHubDuplicateAction.Skip,
            nameof(DataHubLoadMode.UpdateExisting) or nameof(DataHubLoadMode.Upsert) or nameof(DataHubLoadMode.MergeDuplicates)
                => existsInCrm ? DataHubDuplicateAction.Update : DataHubDuplicateAction.CreateNew,
            _ => DataHubDuplicateAction.CreateNew
        };

    private async Task<List<DataHubDuplicateGroupDto>> FindGroupsAsync(
        Guid tenantId, string targetEntity, DataHubDuplicateMatchField field,
        IReadOnlyList<DataHubImportRow> rows, CancellationToken ct)
    {
        var groups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        foreach (var row in rows)
        {
            var data = row.TransformedData.Count > 0 ? row.TransformedData : row.RawData;
            var key = BuildKey(data, field);
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (!groups.TryGetValue(key, out var list))
            {
                list = [];
                groups[key] = list;
            }
            list.Add(row.RowNumber);
        }

        var result = new List<DataHubDuplicateGroupDto>();
        foreach (var (key, rowNums) in groups.Where(g => g.Value.Count > 1))
        {
            result.Add(new DataHubDuplicateGroupDto(
                field.ToString(), key, rowNums.Min(), rowNums,
                await FindExistingEntityIdAsync(tenantId, targetEntity, field, key, ct),
                "Skip"));
        }

        foreach (var row in rows)
        {
            var data = row.TransformedData.Count > 0 ? row.TransformedData : row.RawData;
            var key = BuildKey(data, field);
            if (string.IsNullOrWhiteSpace(key)) continue;
            if (groups.TryGetValue(key, out var list) && list.Count > 1) continue;
            var existing = await FindExistingEntityIdAsync(tenantId, targetEntity, field, key, ct);
            if (existing == null) continue;
            result.Add(new DataHubDuplicateGroupDto(
                field.ToString(), key, row.RowNumber, [row.RowNumber],
                existing, "Update"));
        }

        return result;
    }

    private static string? BuildKey(Dictionary<string, string?> data, DataHubDuplicateMatchField field)
        => field switch
        {
            DataHubDuplicateMatchField.Email => Normalize(data.GetValueOrDefault("Email")),
            DataHubDuplicateMatchField.Phone => NormalizePhone(data.GetValueOrDefault("Phone")),
            DataHubDuplicateMatchField.Company => Normalize(data.GetValueOrDefault("Company")),
            DataHubDuplicateMatchField.NameAndCompany =>
                $"{Normalize(data.GetValueOrDefault("Name"))}|{Normalize(data.GetValueOrDefault("Company"))}",
            _ => null
        };

    private static string? Normalize(string? v) => string.IsNullOrWhiteSpace(v) ? null : v.Trim().ToLowerInvariant();
    private static string? NormalizePhone(string? v)
    {
        if (string.IsNullOrWhiteSpace(v)) return null;
        return new string(v.Where(char.IsDigit).ToArray());
    }

    private async Task<Guid?> FindExistingEntityIdAsync(Guid tenantId, string entityType, DataHubDuplicateMatchField field, string key, CancellationToken ct)
    {
        if (field == DataHubDuplicateMatchField.Email)
        {
            if (entityType == "Customer")
            {
                var id = await _db.Customers.Where(c => c.TenantId == tenantId && c.Email != null && c.Email.ToLower() == key)
                    .Select(c => c.Id).FirstOrDefaultAsync(ct);
                return id == Guid.Empty ? null : id;
            }
            if (entityType == "Lead")
            {
                var id = await _db.Leads.Where(l => l.TenantId == tenantId && l.Email != null && l.Email.ToLower() == key)
                    .Select(l => l.Id).FirstOrDefaultAsync(ct);
                return id == Guid.Empty ? null : id;
            }
        }
        return null;
    }
}
