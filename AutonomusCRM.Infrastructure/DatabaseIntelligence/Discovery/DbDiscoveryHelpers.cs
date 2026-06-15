using System.Text.RegularExpressions;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Discovery;

internal static partial class DbDiscoverySqlGuard
{
    private static readonly string[] Forbidden =
    [
        "INSERT", "UPDATE", "DELETE", "DROP", "ALTER", "CREATE", "TRUNCATE",
        "EXEC", "EXECUTE", "MERGE", "GRANT", "REVOKE", "CALL"
    ];

    public static void EnsureReadOnlyMetadataQuery(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
            throw new InvalidOperationException("Empty SQL is not allowed.");

        var normalized = Whitespace().Replace(sql, " ").Trim();
        if (!normalized.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Only SELECT metadata queries are allowed.");

        foreach (var token in Forbidden)
        {
            if (Regex.IsMatch(normalized, $@"\b{token}\b", RegexOptions.IgnoreCase))
                throw new InvalidOperationException($"Forbidden SQL token: {token}");
        }
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}

internal static class DbRelationshipHeuristics
{
    private static readonly (string Suffix, string[] TargetTables, int Confidence)[] Patterns =
    [
        ("customer_id", ["customers", "customer", "clientes", "cliente"], 85),
        ("client_id", ["clients", "client", "clientes", "cliente"], 85),
        ("contact_id", ["contacts", "contact", "contactos", "contacto"], 82),
        ("account_id", ["accounts", "account", "cuentas", "cuenta"], 80),
        ("company_id", ["companies", "company", "empresas", "empresa"], 84),
        ("empresa_id", ["empresas", "empresa", "companies", "company"], 84),
        ("invoice_id", ["invoices", "invoice", "facturas", "factura"], 78),
        ("order_id", ["orders", "order", "pedidos", "pedido"], 78),
        ("user_id", ["users", "user", "usuarios", "usuario"], 75),
    ];

    public static void ApplyNamingHeuristics(PhysicalSchemaDiscoveryResult result)
    {
        var pkLookup = result.Columns
            .Where(c => c.IsPrimaryKey)
            .GroupBy(c => $"{c.SchemaName}.{c.ObjectName}".ToLowerInvariant())
            .ToDictionary(g => g.Key, g => g.First().ColumnName, StringComparer.OrdinalIgnoreCase);

        var tableLookup = result.Tables
            .Select(t => $"{t.SchemaName}.{t.ObjectName}".ToLowerInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var col in result.Columns.Where(c => c.ColumnName.EndsWith("_id", StringComparison.OrdinalIgnoreCase)))
        {
            var fromKey = $"{col.SchemaName}.{col.ObjectName}";
            foreach (var (suffix, targets, confidence) in Patterns)
            {
                if (!col.ColumnName.Equals(suffix, StringComparison.OrdinalIgnoreCase) &&
                    !col.ColumnName.EndsWith("_" + suffix, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var target in targets)
                {
                    var candidate = result.Tables.FirstOrDefault(t =>
                        t.ObjectName.Equals(target, StringComparison.OrdinalIgnoreCase));
                    if (candidate == null) continue;

                    var toKey = $"{candidate.SchemaName}.{candidate.ObjectName}";
                    if (!pkLookup.TryGetValue(toKey, out var pkCol)) continue;

                    if (result.Relationships.Any(r =>
                            r.FromSchema == col.SchemaName && r.FromTable == col.ObjectName &&
                            r.FromColumn == col.ColumnName &&
                            r.ToSchema == candidate.SchemaName && r.ToTable == candidate.ObjectName))
                        continue;

                    result.Relationships.Add(new PhysicalRelationshipInfo
                    {
                        FromSchema = col.SchemaName,
                        FromTable = col.ObjectName,
                        FromColumn = col.ColumnName,
                        ToSchema = candidate.SchemaName,
                        ToTable = candidate.ObjectName,
                        ToColumn = pkCol,
                        Source = DbRelationshipSource.NamingHeuristic,
                        ConfidencePercent = confidence
                    });
                    break;
                }
            }
        }
    }
}
