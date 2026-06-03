using System.Globalization;
using System.Text;
using AutonomusCRM.Application.DataPlatform;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.DataPlatform;

public sealed class WarehouseExportService : IWarehouseExportService
{
    private readonly ApplicationDbContext _db;

    public WarehouseExportService(ApplicationDbContext db) => _db = db;

    public async Task<byte[]> ExportCustomersCsvAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var rows = await _db.Customers
            .Where(c => c.TenantId == tenantId)
            .Select(c => new { c.Id, c.Name, c.Email, c.Status, c.LifetimeValue, c.CreatedAt })
            .ToListAsync(cancellationToken);

        var sb = new StringBuilder();
        sb.AppendLine("id,name,email,status,lifetime_value,created_at");
        foreach (var r in rows)
        {
            sb.Append(Csv(r.Id.ToString()));
            sb.Append(',');
            sb.Append(Csv(r.Name));
            sb.Append(',');
            sb.Append(Csv(r.Email));
            sb.Append(',');
            sb.Append(Csv(r.Status.ToString()));
            sb.Append(',');
            sb.Append(r.LifetimeValue?.ToString(CultureInfo.InvariantCulture) ?? "");
            sb.Append(',');
            sb.AppendLine(r.CreatedAt.ToString("O"));
        }

        return Encoding.UTF8.GetBytes(sb.ToString());
    }

    private static string Csv(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
