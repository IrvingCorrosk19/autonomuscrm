using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.BusinessDiscovery;

internal static class BusinessEntitySignals
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRx = new(@"^\+?[\d\s\-().]{7,20}$", RegexOptions.Compiled);
    private static readonly Regex SkuRx = new(@"^[A-Z0-9][A-Z0-9\-_.]{2,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex InvoiceNumberRx = new(@"^(INV|FAC|F-|IN-)?[\w\-/]{3,}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TaxIdRx = new(@"^[\d\-A-Z]{8,15}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal sealed record EntityProfile(
        BusinessEntityType Type,
        string DisplayName,
        string[] TableSignals,
        string[] ColumnSignals,
        string[] RelatedTableSignals,
        SamplePattern[] SamplePatterns);

    internal sealed record SamplePattern(string Label, Func<string, bool> Validator);

    internal static readonly EntityProfile[] Profiles =
    [
        new(BusinessEntityType.Customer, "Cliente",
            ["customer", "customers", "cliente", "clientes", "client", "clients", "cli", "cust", "account", "accounts", "cuenta", "cuentas", "buyer", "cliente_master", "customer_master", "tbl_cli", "cust_master"],
            ["customer_id", "cliente_id", "client_id", "customer_name", "cliente_nombre", "cli_name", "cli_email", "customer_email", "name", "nombre", "tax_id", "ruc", "nit", "cif", "national_id", "email", "correo"],
            ["order", "sale", "invoice", "contact"],
            [new("email", EmailRx.IsMatch), new("tax_id", TaxIdRx.IsMatch)]),
        new(BusinessEntityType.Company, "Empresa",
            ["company", "companies", "empresa", "empresas", "organization", "organisation", "org", "business", "firm", "corporate", "compania"],
            ["company_name", "empresa_nombre", "legal_name", "razon_social", "business_name", "org_name", "tax_id", "ruc", "vat_number", "empresa_id"],
            ["contact", "customer", "employee"],
            [new("tax_id", TaxIdRx.IsMatch)]),
        new(BusinessEntityType.Contact, "Contacto",
            ["contact", "contacts", "contacto", "contactos", "person", "persons", "persona", "people", "crm_contacts", "customer_contacts", "contact_person"],
            ["contact_name", "first_name", "last_name", "nombre", "apellido", "email", "correo", "phone", "telefono", "mobile", "customer_id", "company_id"],
            ["customer", "company", "cliente", "empresa"],
            [new("email", EmailRx.IsMatch), new("phone", s => PhoneRx.IsMatch(s) && s.Count(char.IsDigit) >= 7)]),
        new(BusinessEntityType.Sale, "Venta",
            ["sale", "sales", "order", "orders", "venta", "ventas", "pedido", "pedidos", "deal", "deals", "sales_header", "order_header", "ord_hdr", "tbl_ventas", "ventas"],
            ["order_id", "sale_id", "order_date", "sale_date", "order_total", "total_amount", "subtotal", "customer_id", "order_number", "deal_value"],
            ["customer", "cliente", "product", "line", "invoice"],
            []),
        new(BusinessEntityType.Invoice, "Factura",
            ["invoice", "invoices", "factura", "facturas", "billing", "bill", "receipt", "receipts", "inv_hdr", "inv", "facturacion", "invoice_header", "acc_mov"],
            ["invoice_id", "invoice_number", "invoice_date", "factura_numero", "factura_fecha", "total_amount", "subtotal", "tax_amount", "due_date", "customer_id"],
            ["customer", "cliente", "payment", "pago"],
            [new("invoice_number", InvoiceNumberRx.IsMatch)]),
        new(BusinessEntityType.Payment, "Pago",
            ["payment", "payments", "pago", "pagos", "abono", "abonos", "transaction", "transactions", "transaccion", "cash_receipt"],
            ["payment_id", "payment_date", "pago_fecha", "amount", "monto", "payment_method", "invoice_id", "factura_id", "reference_number"],
            ["invoice", "factura", "customer"],
            []),
        new(BusinessEntityType.Product, "Producto",
            ["product", "products", "producto", "productos", "item", "items", "sku", "catalog", "articulo", "articulos", "inventory", "order_lines", "line_item"],
            ["product_id", "sku", "product_name", "product_code", "unit_price", "price", "precio", "barcode", "stock_quantity"],
            ["order", "sale", "category"],
            [new("sku", SkuRx.IsMatch)]),
        new(BusinessEntityType.Activity, "Actividad",
            ["activity", "activities", "actividad", "actividades", "task", "tasks", "tarea", "call", "calls", "meeting", "meetings", "event", "events", "note", "notes"],
            ["activity_id", "subject", "activity_date", "due_date", "activity_type", "call_duration", "contact_id", "customer_id", "assigned_to"],
            ["contact", "customer", "company"],
            []),
        new(BusinessEntityType.User, "Usuario",
            ["user", "users", "usuario", "usuarios", "employee", "employees", "staff", "agent", "agents", "login", "membership"],
            ["username", "user_email", "password_hash", "role", "last_login", "employee_id"],
            ["company", "team"],
            [new("email", EmailRx.IsMatch)])
    ];

    internal static string NormalizeIdentifier(string value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var s = value.Trim().ToLowerInvariant();
        s = s.Replace('-', '_').Replace(' ', '_');
        s = Regex.Replace(s, @"^(tbl|tb|dim|fact|stg|raw|crm|erp|acc|mv)_", "");
        s = Regex.Replace(s, @"_(hdr|header|det|detail|master|data|mov|\d{4})$", "");
        return s;
    }

    internal static HashSet<string> Tokenize(string value)
    {
        var normalized = NormalizeIdentifier(value);
        return normalized.Split('_', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
    }

    internal static double ScoreTableName(string tableName, EntityProfile profile)
    {
        var normalized = NormalizeIdentifier(tableName);
        var tokens = Tokenize(tableName);
        var best = 0.0;

        foreach (var signal in profile.TableSignals)
        {
            var sig = NormalizeIdentifier(signal);
            if (string.Equals(normalized, sig, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 100);
            else if (normalized.Contains(sig, StringComparison.OrdinalIgnoreCase))
                best = Math.Max(best, 88);
            else if (tokens.Overlaps(sig.Split('_', StringSplitOptions.RemoveEmptyEntries)))
                best = Math.Max(best, 78);
        }

        return best;
    }

    internal static double ScoreColumns(
        IReadOnlyList<BusinessDiscoveryColumnInput> columns,
        EntityProfile profile,
        List<string> reasons)
    {
        if (columns.Count == 0) return 0;
        var hits = 0;
        foreach (var col in columns)
        {
            var normalized = NormalizeIdentifier(col.ColumnName);
            foreach (var signal in profile.ColumnSignals)
            {
                var sig = NormalizeIdentifier(signal);
                if (normalized.Contains(sig, StringComparison.OrdinalIgnoreCase) ||
                    Tokenize(col.ColumnName).Contains(sig))
                {
                    hits++;
                    reasons.Add($"contiene columna {col.ColumnName}");
                    break;
                }
            }
        }

        return Math.Min(100, hits * 22.0);
    }

    internal static double ScoreRelationships(
        string schema,
        string table,
        BusinessDiscoveryCatalogInput catalog,
        Dictionary<string, BusinessEntityType> tableEntityHints,
        EntityProfile profile,
        List<string> reasons)
    {
        var outgoing = catalog.Relationships.Where(r =>
            string.Equals(r.FromSchema, schema, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(r.FromTable, table, StringComparison.OrdinalIgnoreCase)).ToList();

        if (outgoing.Count == 0) return 0;

        var score = 0.0;
        foreach (var rel in outgoing)
        {
            var targetKey = BusinessDiscoveryCatalogInput.TableKey(rel.ToSchema, rel.ToTable);
            if (tableEntityHints.TryGetValue(targetKey, out var targetType))
            {
                foreach (var related in profile.RelatedTableSignals)
                {
                    if (targetType.ToString().Contains(related, StringComparison.OrdinalIgnoreCase) ||
                        NormalizeIdentifier(rel.ToTable).Contains(related, StringComparison.OrdinalIgnoreCase))
                    {
                        score = Math.Max(score, 75);
                        reasons.Add($"tiene relación hacia {rel.ToTable}");
                    }
                }
            }

            if (profile.ColumnSignals.Any(s => NormalizeIdentifier(rel.FromColumn).Contains(NormalizeIdentifier(s), StringComparison.OrdinalIgnoreCase)))
            {
                score = Math.Max(score, 65);
                reasons.Add($"columna {rel.FromColumn} sugiere vínculo de negocio");
            }
        }

        return score;
    }

    internal static double ScoreSamples(
        IReadOnlyList<IReadOnlyDictionary<string, string?>>? rows,
        IReadOnlyList<BusinessDiscoveryColumnInput> columns,
        EntityProfile profile,
        List<string> reasons)
    {
        if (rows == null || rows.Count == 0) return 0;

        var best = 0.0;
        foreach (var col in columns)
        {
            var samples = rows.Select(r => r.TryGetValue(col.ColumnName, out var v) ? v : null)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Take(8)
                .ToList();
            if (samples.Count == 0) continue;

            foreach (var pattern in profile.SamplePatterns)
            {
                var matches = samples.Count(s => pattern.Validator(s!));
                if (matches >= Math.Max(1, samples.Count / 2))
                {
                    best = Math.Max(best, 85);
                    reasons.Add($"muestras en {col.ColumnName} coinciden con patrón {pattern.Label}");
                }
            }
        }

        return best;
    }

    internal static string DisplayName(BusinessEntityType type) =>
        Profiles.FirstOrDefault(p => p.Type == type)?.DisplayName ??
        (type == BusinessEntityType.Unknown ? "Desconocido" : type.ToString());

    internal static string BuildExplanation(BusinessEntityType type, int confidence, List<string> reasons)
    {
        var name = DisplayName(type);
        var sb = new StringBuilder($"Creemos que esto es {name}. Confidence: {confidence}%.");
        if (reasons.Count > 0)
            sb.Append(' ').Append(string.Join("; ", reasons.Distinct().Take(6)));
        return sb.ToString();
    }

    private static bool Overlaps(this HashSet<string> tokens, string[] other)
        => other.Any(t => tokens.Contains(t));
}
