using Microsoft.EntityFrameworkCore;

namespace AutonomusCRM.Infrastructure.Persistence;

internal static class PostgresJsonbQuery
{
    public static async Task<int> CountJsonbKeyAsync(
        ApplicationDbContext context,
        string tableName,
        Guid tenantId,
        string metadataKey,
        CancellationToken cancellationToken = default)
    {
        return await context.Database
            .SqlQueryRaw<int>(
                """
                SELECT COUNT(*)::int AS "Value"
                FROM "Customers"
                WHERE "TenantId" = {0} AND "Metadata" ? {1}
                """.Replace("\"Customers\"", $"\"{tableName}\""),
                tenantId,
                metadataKey)
            .SingleAsync(cancellationToken);
    }
}
