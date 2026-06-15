using System.Text.RegularExpressions;
using AutonomusCRM.Application.DatabaseIntelligence;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence.Health;

internal static class DataHealthDuplicateDetector
{
    private static readonly Regex EmailRx = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex PhoneRx = new(@"^\+?[\d\s\-().]{7,20}$", RegexOptions.Compiled);
    private static readonly Regex TaxIdRx = new(@"^[\d\-A-Z]{8,15}$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    internal static IReadOnlyList<(string Field, string Key, IReadOnlyList<int> RowIndexes)> FindDuplicates(
        DataHealthTableContext table,
        params string[] columnHints)
    {
        var results = new List<(string, string, IReadOnlyList<int>)>();
        foreach (var hint in columnHints)
        {
            var col = table.Columns.FirstOrDefault(c =>
                c.ColumnName.Contains(hint, StringComparison.OrdinalIgnoreCase));
            if (col == null) continue;

            var groups = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < table.Rows.Count; i++)
            {
                var row = table.Rows[i];
                if (!row.TryGetValue(col.ColumnName, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;
                var key = NormalizeKey(hint, raw);
                if (string.IsNullOrWhiteSpace(key)) continue;
                if (!groups.TryGetValue(key, out var list))
                {
                    list = [];
                    groups[key] = list;
                }
                list.Add(i);
            }

            foreach (var (key, indexes) in groups.Where(g => g.Value.Count > 1))
                results.Add((col.ColumnName, key, indexes));
        }

        return results;
    }

    private static string? NormalizeKey(string hint, string value)
    {
        if (hint.Contains("email", StringComparison.OrdinalIgnoreCase))
            return value.Trim().ToLowerInvariant();
        if (hint.Contains("phone", StringComparison.OrdinalIgnoreCase) || hint.Contains("telefono", StringComparison.OrdinalIgnoreCase))
            return new string(value.Where(char.IsDigit).ToArray());
        if (hint.Contains("tax", StringComparison.OrdinalIgnoreCase) || hint.Contains("ruc", StringComparison.OrdinalIgnoreCase))
            return value.Trim().ToUpperInvariant();
        return value.Trim().ToLowerInvariant();
    }

    internal static bool IsValidEmail(string? v) => !string.IsNullOrWhiteSpace(v) && EmailRx.IsMatch(v);
    internal static bool IsValidPhone(string? v) => !string.IsNullOrWhiteSpace(v) && PhoneRx.IsMatch(v) && v.Count(char.IsDigit) >= 7;
    internal static bool IsValidTaxId(string? v) => !string.IsNullOrWhiteSpace(v) && TaxIdRx.IsMatch(v);
    internal static bool IsValidAmount(string? v) => decimal.TryParse(v?.Replace(",", "."), System.Globalization.NumberStyles.Any,
        System.Globalization.CultureInfo.InvariantCulture, out var d) && d >= 0;
    internal static bool IsValidDate(string? v) => DateTime.TryParse(v, out _);
}

public sealed class DataHealthEngine : IDataHealthEngine
{
    public DataHealthScanResult Scan(DataHealthScanInput input, IProgress<DataHealthProgress>? progress = null)
    {
        var findings = new List<DataHealthFindingDto>();
        var entityScores = new Dictionary<BusinessEntityType, EntityScoreAccumulator>();

        progress?.Report(new DataHealthProgress(DataHealthStages.ScanningCustomers, 10, "Customer"));
        ScanEntityTables(input, BusinessEntityType.Customer, findings, entityScores);
        ScanEntityTables(input, BusinessEntityType.Company, findings, entityScores);

        progress?.Report(new DataHealthProgress(DataHealthStages.ScanningInvoices, 35, "Invoice"));
        ScanEntityTables(input, BusinessEntityType.Invoice, findings, entityScores);
        ScanEntityTables(input, BusinessEntityType.Sale, findings, entityScores);

        progress?.Report(new DataHealthProgress(DataHealthStages.ScanningPayments, 55, "Payment"));
        ScanEntityTables(input, BusinessEntityType.Payment, findings, entityScores);

        progress?.Report(new DataHealthProgress(DataHealthStages.ScanningProducts, 70, "Product"));
        ScanEntityTables(input, BusinessEntityType.Contact, findings, entityScores);
        ScanEntityTables(input, BusinessEntityType.Product, findings, entityScores);
        ScanEntityTables(input, BusinessEntityType.Activity, findings, entityScores);

        DetectOrphans(input, findings, entityScores);
        DetectBrokenRelationships(input, findings, entityScores);
        DetectBusinessConsistency(input, findings, entityScores);

        progress?.Report(new DataHealthProgress(DataHealthStages.CalculatingScore, 90));
        var scores = BuildScores(entityScores, findings);
        var global = scores.Count == 0 ? 100 : (int)Math.Round(scores.Average(s => s.Score));

        progress?.Report(new DataHealthProgress(DataHealthStages.Completed, 100,
            Message: $"Health scan completed — global score {global}"));

        return new DataHealthScanResult
        {
            Findings = findings,
            Scores = scores,
            GlobalScore = global
        };
    }

    private static void ScanEntityTables(
        DataHealthScanInput input,
        BusinessEntityType entityType,
        List<DataHealthFindingDto> findings,
        Dictionary<BusinessEntityType, EntityScoreAccumulator> entityScores)
    {
        foreach (var table in input.Tables.Where(t => t.EntityType == entityType))
        {
            var acc = GetAccumulator(entityScores, entityType);
            acc.TablesScanned++;
            ScanColumnQuality(table, entityType, findings, acc);
            ScanDuplicates(table, entityType, findings, acc);
        }
    }

    private static void ScanColumnQuality(
        DataHealthTableContext table,
        BusinessEntityType entityType,
        List<DataHealthFindingDto> findings,
        EntityScoreAccumulator acc)
    {
        if (table.Rows.Count == 0) return;

        var emailCols = table.Columns.Where(c => c.ColumnName.Contains("email", StringComparison.OrdinalIgnoreCase) ||
                                                  c.ColumnName.Contains("correo", StringComparison.OrdinalIgnoreCase)).ToList();
        var phoneCols = table.Columns.Where(c => c.ColumnName.Contains("phone", StringComparison.OrdinalIgnoreCase) ||
                                                  c.ColumnName.Contains("telefono", StringComparison.OrdinalIgnoreCase)).ToList();
        var amountCols = table.Columns.Where(c => c.ColumnName.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                                                   c.ColumnName.Contains("total", StringComparison.OrdinalIgnoreCase) ||
                                                   c.ColumnName.Contains("monto", StringComparison.OrdinalIgnoreCase)).ToList();
        var nameCols = table.Columns.Where(c => c.ColumnName.Contains("name", StringComparison.OrdinalIgnoreCase) ||
                                                 c.ColumnName.Contains("nombre", StringComparison.OrdinalIgnoreCase)).ToList();

        var emptyNames = CountEmpty(table, nameCols);
        if (emptyNames > 0 && entityType is BusinessEntityType.Customer or BusinessEntityType.Contact or BusinessEntityType.Company)
        {
            acc.CompletenessPenalty += emptyNames * 8;
            findings.Add(MakeFinding(entityType, DataHealthFindingSeverity.High, DataHealthFindingCategory.IncompleteData,
                $"{EntityLabel(entityType)} incompletos",
                $"Hay {emptyNames} registros sin nombre identificable.",
                "Dificulta identificar clientes y contactos en operaciones diarias.",
                $"{emptyNames} filas con nombre vacío en {table.SchemaName}.{table.TableName}",
                "Completar nombres o vincular con otra fuente de datos.",
                table.SchemaName, table.TableName, emptyNames));
        }

        foreach (var col in emailCols)
        {
            var invalid = table.Rows.Count(r => r.TryGetValue(col.ColumnName, out var v) && !string.IsNullOrWhiteSpace(v) &&
                                                !DataHealthDuplicateDetector.IsValidEmail(v));
            var empty = table.Rows.Count(r => !r.TryGetValue(col.ColumnName, out var v) || string.IsNullOrWhiteSpace(v));
            if (invalid > 0)
            {
                acc.ValidityPenalty += invalid * 10;
                findings.Add(MakeFinding(entityType, DataHealthFindingSeverity.Medium, DataHealthFindingCategory.InvalidFormat,
                    "Correos electrónicos inválidos",
                    $"{invalid} registros tienen formato de correo incorrecto.",
                    "Los envíos y campañas pueden fallar o llegar a destinatarios equivocados.",
                    $"{invalid} correos inválidos en columna {col.ColumnName}",
                    "Validar y corregir direcciones de correo.",
                    table.SchemaName, table.TableName, invalid));
            }
            if (empty > 0 && entityType == BusinessEntityType.Contact)
                acc.CompletenessPenalty += empty * 5;
        }

        foreach (var col in phoneCols)
        {
            var invalid = table.Rows.Count(r => r.TryGetValue(col.ColumnName, out var v) && !string.IsNullOrWhiteSpace(v) &&
                                                !DataHealthDuplicateDetector.IsValidPhone(v));
            if (invalid > 0)
            {
                acc.ValidityPenalty += invalid * 6;
                findings.Add(MakeFinding(entityType, DataHealthFindingSeverity.Low, DataHealthFindingCategory.InvalidFormat,
                    "Teléfonos con formato incorrecto",
                    $"{invalid} números no cumplen un formato válido.",
                    "Las llamadas y mensajes pueden no llegar al contacto correcto.",
                    $"{invalid} teléfonos inválidos en {col.ColumnName}",
                    "Normalizar números con código de país.",
                    table.SchemaName, table.TableName, invalid));
            }
        }

        foreach (var col in amountCols)
        {
            var negative = table.Rows.Count(r => r.TryGetValue(col.ColumnName, out var v) && !string.IsNullOrWhiteSpace(v) &&
                decimal.TryParse(v.Replace(",", "."), System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out var d) && d < 0);
            if (negative > 0)
            {
                acc.ValidityPenalty += negative * 12;
                findings.Add(MakeFinding(entityType, DataHealthFindingSeverity.High, DataHealthFindingCategory.BusinessInconsistency,
                    "Montos negativos detectados",
                    $"{negative} registros muestran valores negativos donde no deberían.",
                    "Distorsiona reportes de ingresos y conciliación financiera.",
                    $"{negative} montos negativos en {col.ColumnName}",
                    "Revisar facturas o pagos con importes incorrectos.",
                    table.SchemaName, table.TableName, negative));
            }
        }

        acc.TotalRows += table.Rows.Count;
    }

    private static void ScanDuplicates(
        DataHealthTableContext table,
        BusinessEntityType entityType,
        List<DataHealthFindingDto> findings,
        EntityScoreAccumulator acc)
    {
        var hints = entityType switch
        {
            BusinessEntityType.Customer or BusinessEntityType.Company => new[] { "email", "tax", "ruc" },
            BusinessEntityType.Contact => new[] { "email", "phone" },
            BusinessEntityType.Invoice => new[] { "invoice_number", "factura" },
            BusinessEntityType.Payment => new[] { "reference", "payment" },
            _ => Array.Empty<string>()
        };

        var dupes = DataHealthDuplicateDetector.FindDuplicates(table, hints);
        foreach (var (field, key, indexes) in dupes)
        {
            acc.DuplicatePenalty += indexes.Count * 15;
            var title = entityType switch
            {
                BusinessEntityType.Customer => "Clientes duplicados",
                BusinessEntityType.Contact => "Contactos duplicados",
                BusinessEntityType.Company => "Empresas duplicadas",
                BusinessEntityType.Invoice => "Números de factura duplicados",
                BusinessEntityType.Payment => "Pagos duplicados",
                _ => "Registros duplicados"
            };
            findings.Add(MakeFinding(entityType, DataHealthFindingSeverity.High, DataHealthFindingCategory.Duplicate,
                title,
                $"{indexes.Count} registros comparten el mismo valor en {field}.",
                "Riesgo de facturación doble, seguimiento confuso y reportes inflados.",
                $"Clave duplicada '{key}' en {table.SchemaName}.{table.TableName}",
                "Unificar registros duplicados o marcar el registro principal.",
                table.SchemaName, table.TableName, indexes.Count));
        }
    }

    private static void DetectOrphans(
        DataHealthScanInput input,
        List<DataHealthFindingDto> findings,
        Dictionary<BusinessEntityType, EntityScoreAccumulator> entityScores)
    {
        foreach (var rel in input.Relationships)
        {
            var child = input.Tables.FirstOrDefault(t =>
                t.SchemaName == rel.FromSchema && t.TableName == rel.FromTable);
            var parent = input.Tables.FirstOrDefault(t =>
                t.SchemaName == rel.ToSchema && t.TableName == rel.ToTable);
            if (child == null || parent == null || child.Rows.Count == 0) continue;

            var parentKeys = parent.Rows
                .Select(r => r.TryGetValue(rel.ToColumn, out var v) ? v?.Trim() : null)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var orphans = child.Rows.Count(r =>
            {
                if (!r.TryGetValue(rel.FromColumn, out var fk) || string.IsNullOrWhiteSpace(fk)) return true;
                return !parentKeys.Contains(fk.Trim());
            });

            if (orphans == 0) continue;

            var (title, impact, severity, childEntity) = rel.FromEntity switch
            {
                BusinessEntityType.Invoice when rel.ToEntity == BusinessEntityType.Customer =>
                    ("Facturas sin cliente", "No se puede atribuir ingresos ni hacer seguimiento comercial.", DataHealthFindingSeverity.Critical, BusinessEntityType.Invoice),
                BusinessEntityType.Payment when rel.ToEntity == BusinessEntityType.Invoice =>
                    ("Pagos sin factura", "Dificulta la conciliación y el cierre contable.", DataHealthFindingSeverity.Critical, BusinessEntityType.Payment),
                BusinessEntityType.Contact when rel.ToEntity is BusinessEntityType.Customer or BusinessEntityType.Company =>
                    ("Contactos sin empresa", "El equipo no sabe a qué organización pertenece el contacto.", DataHealthFindingSeverity.High, BusinessEntityType.Contact),
                BusinessEntityType.Sale when rel.ToEntity == BusinessEntityType.Customer =>
                    ("Ventas sin cliente", "Las oportunidades no están vinculadas a un comprador.", DataHealthFindingSeverity.High, BusinessEntityType.Sale),
                _ => ($"Registros huérfanos en {rel.FromTable}", "Relaciones de negocio incompletas.", DataHealthFindingSeverity.Medium, rel.FromEntity)
            };

            GetAccumulator(entityScores, childEntity).ConsistencyPenalty += orphans * 12;
            findings.Add(MakeFinding(childEntity, severity, DataHealthFindingCategory.Orphan,
                title,
                $"{orphans} registros en {child.TableName} no tienen vínculo válido hacia {parent.TableName}.",
                impact,
                $"{orphans} huérfanos — FK {rel.FromColumn} → {rel.ToTable}.{rel.ToColumn}",
                "Vincular cada registro con su cliente, factura o empresa correspondiente.",
                child.SchemaName, child.TableName, orphans));
        }
    }

    private static void DetectBrokenRelationships(
        DataHealthScanInput input,
        List<DataHealthFindingDto> findings,
        Dictionary<BusinessEntityType, EntityScoreAccumulator> entityScores)
    {
        foreach (var rel in input.Relationships)
        {
            var child = input.Tables.FirstOrDefault(t => t.SchemaName == rel.FromSchema && t.TableName == rel.FromTable);
            if (child == null) continue;

            var broken = child.Rows.Count(r =>
                r.TryGetValue(rel.FromColumn, out var fk) && !string.IsNullOrWhiteSpace(fk) &&
                fk!.Contains("INVALID", StringComparison.OrdinalIgnoreCase));

            if (broken == 0) continue;

            GetAccumulator(entityScores, rel.FromEntity).ConsistencyPenalty += broken * 15;
            findings.Add(MakeFinding(rel.FromEntity, DataHealthFindingSeverity.Critical, DataHealthFindingCategory.BrokenRelationship,
                "Relaciones de datos rotas",
                $"{broken} registros apuntan a referencias que no existen.",
                "Los reportes y automatizaciones pueden producir resultados incorrectos.",
                $"{broken} referencias inválidas en {rel.FromColumn}",
                "Corregir o eliminar referencias inexistentes.",
                child.SchemaName, child.TableName, broken));
        }
    }

    private static void DetectBusinessConsistency(
        DataHealthScanInput input,
        List<DataHealthFindingDto> findings,
        Dictionary<BusinessEntityType, EntityScoreAccumulator> entityScores)
    {
        var invoices = input.Tables.Where(t => t.EntityType == BusinessEntityType.Invoice).ToList();
        var lines = input.Tables.Where(t => t.TableName.Contains("line", StringComparison.OrdinalIgnoreCase) ||
                                             t.TableName.Contains("det", StringComparison.OrdinalIgnoreCase)).ToList();
        var payments = input.Tables.Where(t => t.EntityType == BusinessEntityType.Payment).ToList();

        foreach (var inv in invoices)
        {
            var totalCol = inv.Columns.FirstOrDefault(c => c.ColumnName.Contains("total", StringComparison.OrdinalIgnoreCase));
            if (totalCol == null) continue;

            foreach (var lineTable in lines)
            {
                var lineTotal = lineTable.Rows
                    .Where(r => r.TryGetValue("invoice_id", out var id) || r.TryGetValue("factura_id", out id))
                    .GroupBy(r => r.TryGetValue("invoice_id", out var i) ? i : r.GetValueOrDefault("factura_id"))
                    .ToList();

                foreach (var group in lineTotal)
                {
                    var invRow = inv.Rows.FirstOrDefault(r =>
                        (r.TryGetValue("invoice_id", out var id) ? id : r.GetValueOrDefault("id")) == group.Key);
                    if (invRow == null || !invRow.TryGetValue(totalCol.ColumnName, out var invTotalStr)) continue;
                    if (!decimal.TryParse(invTotalStr?.Replace(",", "."), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var invTotal)) continue;

                    decimal lineSum = 0;
                    foreach (var lr in group)
                    {
                        var amtCol = lr.Keys.FirstOrDefault(k => k.Contains("amount", StringComparison.OrdinalIgnoreCase) ||
                                                                  k.Contains("total", StringComparison.OrdinalIgnoreCase) ||
                                                                  k.Contains("price", StringComparison.OrdinalIgnoreCase));
                        if (amtCol != null && decimal.TryParse(lr[amtCol]?.Replace(",", "."),
                                System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var la))
                            lineSum += la;
                    }

                    if (lineSum > 0 && Math.Abs(invTotal - lineSum) > 0.01m)
                    {
                        GetAccumulator(entityScores, BusinessEntityType.Invoice).ConsistencyPenalty += 20;
                        findings.Add(MakeFinding(BusinessEntityType.Invoice, DataHealthFindingSeverity.High,
                            DataHealthFindingCategory.BusinessInconsistency,
                            "Total de factura no coincide con líneas",
                            $"La factura {group.Key} muestra {invTotal:N2} pero las líneas suman {lineSum:N2}.",
                            "Riesgo de facturación incorrecta y disputas con clientes.",
                            $"Diferencia {Math.Abs(invTotal - lineSum):N2} en factura {group.Key}",
                            "Alinear el total de la factura con la suma de sus líneas.",
                            inv.SchemaName, inv.TableName, 1));
                    }
                }
            }
        }

        foreach (var pay in payments)
        {
            var amountCol = pay.Columns.FirstOrDefault(c => c.ColumnName.Contains("amount", StringComparison.OrdinalIgnoreCase));
            var invFkCol = pay.Columns.FirstOrDefault(c => c.ColumnName.Contains("invoice", StringComparison.OrdinalIgnoreCase));
            if (amountCol == null || invFkCol == null) continue;

            foreach (var inv in invoices)
            {
                var invTotalCol = inv.Columns.FirstOrDefault(c => c.ColumnName.Contains("total", StringComparison.OrdinalIgnoreCase));
                if (invTotalCol == null) continue;

                foreach (var prow in pay.Rows)
                {
                    if (!prow.TryGetValue(invFkCol.ColumnName, out var invId) || string.IsNullOrWhiteSpace(invId)) continue;
                    if (!prow.TryGetValue(amountCol.ColumnName, out var payAmtStr)) continue;
                    var invRow = inv.Rows.FirstOrDefault(r =>
                        (r.TryGetValue("invoice_id", out var id) ? id : r.GetValueOrDefault("id")) == invId);
                    if (invRow == null || !invRow.TryGetValue(invTotalCol.ColumnName, out var invAmtStr)) continue;

                    if (decimal.TryParse(payAmtStr?.Replace(",", "."), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var payAmt) &&
                        decimal.TryParse(invAmtStr?.Replace(",", "."), System.Globalization.NumberStyles.Any,
                            System.Globalization.CultureInfo.InvariantCulture, out var invAmt) &&
                        payAmt > invAmt)
                    {
                        GetAccumulator(entityScores, BusinessEntityType.Payment).ConsistencyPenalty += 15;
                        findings.Add(MakeFinding(BusinessEntityType.Payment, DataHealthFindingSeverity.Critical,
                            DataHealthFindingCategory.BusinessInconsistency,
                            "Pago mayor que la factura",
                            $"Un pago de {payAmt:N2} supera el total de factura {invAmt:N2}.",
                            "Indica sobrepago o error de registro que afecta la conciliación.",
                            $"Pago {payAmt:N2} > factura {invAmt:N2} (ref {invId})",
                            "Verificar el monto del pago o dividir entre varias facturas.",
                            pay.SchemaName, pay.TableName, 1));
                    }
                }
            }
        }
    }

    private static List<DataHealthScoreDto> BuildScores(
        Dictionary<BusinessEntityType, EntityScoreAccumulator> acc,
        List<DataHealthFindingDto> findings)
    {
        var entityTypes = new[]
        {
            BusinessEntityType.Customer, BusinessEntityType.Company, BusinessEntityType.Contact,
            BusinessEntityType.Invoice, BusinessEntityType.Payment, BusinessEntityType.Product, BusinessEntityType.Sale
        };

        var scores = new List<DataHealthScoreDto>();
        foreach (var et in entityTypes)
        {
            if (!acc.TryGetValue(et, out var a) || a.TablesScanned == 0) continue;
            var completeness = Math.Clamp(100 - a.CompletenessPenalty, 0, 100);
            var validity = Math.Clamp(100 - a.ValidityPenalty, 0, 100);
            var consistency = Math.Clamp(100 - a.ConsistencyPenalty, 0, 100);
            var duplicate = Math.Clamp(100 - a.DuplicatePenalty, 0, 100);
            var entityFindings = findings.Where(f => f.EntityType == et).ToList();
            var severityPenalty = entityFindings.Sum(f => f.Severity switch
            {
                DataHealthFindingSeverity.Critical => 15,
                DataHealthFindingSeverity.High => 8,
                DataHealthFindingSeverity.Medium => 4,
                _ => 2
            });
            var overall = Math.Clamp((int)Math.Round(
                completeness * 0.30 + validity * 0.25 + consistency * 0.25 + duplicate * 0.20 - severityPenalty * 0.1), 0, 100);

            scores.Add(new DataHealthScoreDto(et, overall, DataHealthScoreBand.Label(overall),
                completeness, validity, consistency, duplicate));
        }

        return scores;
    }

    private static int CountEmpty(DataHealthTableContext table, IReadOnlyList<DataHealthColumnContext> cols)
    {
        if (cols.Count == 0) return 0;
        return table.Rows.Count(r => cols.All(c =>
            !r.TryGetValue(c.ColumnName, out var v) || string.IsNullOrWhiteSpace(v)));
    }

    private static EntityScoreAccumulator GetAccumulator(Dictionary<BusinessEntityType, EntityScoreAccumulator> dict, BusinessEntityType et)
    {
        if (!dict.TryGetValue(et, out var acc))
        {
            acc = new EntityScoreAccumulator();
            dict[et] = acc;
        }
        return acc;
    }

    private static DataHealthFindingDto MakeFinding(
        BusinessEntityType? entityType, string severity, string category,
        string title, string explanation, string impact, string evidence, string recommendation,
        string schema, string table, int count) => new(
        Guid.NewGuid(), entityType, severity, category, title, explanation, impact, evidence, recommendation,
        schema, table, count);

    private static string EntityLabel(BusinessEntityType type) => type switch
    {
        BusinessEntityType.Customer => "Clientes",
        BusinessEntityType.Company => "Empresas",
        BusinessEntityType.Contact => "Contactos",
        BusinessEntityType.Invoice => "Facturas",
        BusinessEntityType.Payment => "Pagos",
        BusinessEntityType.Product => "Productos",
        BusinessEntityType.Sale => "Ventas",
        _ => type.ToString()
    };

    private sealed class EntityScoreAccumulator
    {
        public int TablesScanned;
        public int TotalRows;
        public int CompletenessPenalty;
        public int ValidityPenalty;
        public int ConsistencyPenalty;
        public int DuplicatePenalty;
    }
}
