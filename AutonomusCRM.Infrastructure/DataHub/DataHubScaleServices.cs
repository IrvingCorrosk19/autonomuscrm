using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using NpgsqlTypes;

namespace AutonomusCRM.Infrastructure.DataHub;

public static class DataHubBulkStaging
{
    private const string CopySql = """
        COPY "DataHubImportRows"
        ("Id", "JobId", "TenantId", "RowNumber", "BatchNumber", "Status", "RawData", "TransformedData", "CreatedAt")
        FROM STDIN (FORMAT BINARY)
        """;

    public static async Task<int> BulkInsertRowsCopyAsync(
        ApplicationDbContext db, IReadOnlyList<DataHubImportRow> rows, CancellationToken cancellationToken = default)
    {
        if (rows.Count == 0) return 0;

        var conn = (NpgsqlConnection)db.Database.GetDbConnection();
        if (conn.State != System.Data.ConnectionState.Open)
            await conn.OpenAsync(cancellationToken);

        await using var importer = await conn.BeginBinaryImportAsync(CopySql, cancellationToken);
        foreach (var row in rows)
        {
            await importer.StartRowAsync(cancellationToken);
            await importer.WriteAsync(row.Id, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(row.JobId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(row.TenantId, NpgsqlDbType.Uuid, cancellationToken);
            await importer.WriteAsync(row.RowNumber, NpgsqlDbType.Integer, cancellationToken);
            if (row.BatchNumber.HasValue)
                await importer.WriteAsync(row.BatchNumber.Value, NpgsqlDbType.Integer, cancellationToken);
            else
                await importer.WriteNullAsync(cancellationToken);
            await importer.WriteAsync(row.Status, NpgsqlDbType.Text, cancellationToken);
            await importer.WriteAsync(JsonSerializer.Serialize(row.RawData), NpgsqlDbType.Jsonb, cancellationToken);
            var transformed = row.TransformedData.Count > 0 ? JsonSerializer.Serialize(row.TransformedData) : "{}";
            await importer.WriteAsync(transformed, NpgsqlDbType.Jsonb, cancellationToken);
            await importer.WriteAsync(row.CreatedAt, NpgsqlDbType.TimestampTz, cancellationToken);
        }

        await importer.CompleteAsync(cancellationToken);
        return rows.Count;
    }

    public static DataHubScaleMetricsDto Measure(Action<int> operation, int rowCount, string operationName)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();
        var before = GC.GetTotalMemory(forceFullCollection: true);
        var sw = Stopwatch.StartNew();
        operation(rowCount);
        sw.Stop();
        var after = GC.GetTotalMemory(forceFullCollection: false);
        var rps = rowCount / Math.Max(sw.Elapsed.TotalSeconds, 0.001);
        return new DataHubScaleMetricsDto(rowCount, Math.Max(0, after - before), rps, sw.Elapsed, operationName);
    }
}

public static class DataHubExportStreaming
{
    public static async Task WriteCsvHeaderAsync(Stream output, IReadOnlyList<string> columns, CancellationToken cancellationToken)
    {
        var line = string.Join(",", columns.Select(CsvEscape)) + "\n";
        await output.WriteAsync(Encoding.UTF8.GetBytes(line), cancellationToken);
    }

    public static async Task WriteCsvRowAsync(Stream output, IReadOnlyList<string> columns, Dictionary<string, string?> row, CancellationToken cancellationToken)
    {
        var line = string.Join(",", columns.Select(c => CsvEscape(row.GetValueOrDefault(c) ?? ""))) + "\n";
        await output.WriteAsync(Encoding.UTF8.GetBytes(line), cancellationToken);
    }

    public static async Task WriteJsonArrayStartAsync(Stream output, CancellationToken cancellationToken)
        => await output.WriteAsync("["u8.ToArray(), cancellationToken);

    public static async Task WriteJsonRowAsync(Stream output, Dictionary<string, string?> row, bool isFirst, CancellationToken cancellationToken)
    {
        if (!isFirst) await output.WriteAsync(","u8.ToArray(), cancellationToken);
        var json = JsonSerializer.SerializeToUtf8Bytes(row);
        await output.WriteAsync(json, cancellationToken);
    }

    public static async Task WriteJsonArrayEndAsync(Stream output, CancellationToken cancellationToken)
        => await output.WriteAsync("]"u8.ToArray(), cancellationToken);

    public static async Task WriteXlsxStreamAsync(
        Stream output,
        IReadOnlyList<string> columns,
        IAsyncEnumerable<Dictionary<string, string?>> rows,
        CancellationToken cancellationToken)
    {
        using var document = SpreadsheetDocument.Create(output, SpreadsheetDocumentType.Workbook, autoSave: false);
        var workbookPart = document.AddWorkbookPart();
        workbookPart.Workbook = new Workbook();
        var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
        using var writer = OpenXmlWriter.Create(worksheetPart);
        writer.WriteStartElement(new Worksheet());
        writer.WriteStartElement(new SheetData());

        WriteXlsxRow(writer, columns.Select(c => (string?)c));

        await foreach (var row in rows.WithCancellation(cancellationToken))
        {
            WriteXlsxRow(writer, columns.Select(c => row.GetValueOrDefault(c)));
        }

        writer.WriteEndElement();
        writer.WriteEndElement();
        writer.Close();

        var sheets = workbookPart.Workbook.AppendChild(new Sheets());
        sheets.Append(new Sheet
        {
            Id = workbookPart.GetIdOfPart(worksheetPart),
            SheetId = 1,
            Name = "Export"
        });
        workbookPart.Workbook.Save();
        document.Save();
    }

    private static void WriteXlsxRow(OpenXmlWriter writer, IEnumerable<string?> values)
    {
        writer.WriteStartElement(new Row());
        foreach (var value in values)
        {
            writer.WriteStartElement(new Cell { DataType = CellValues.String });
            writer.WriteElement(new CellValue(value ?? string.Empty));
            writer.WriteEndElement();
        }
        writer.WriteEndElement();
    }

    private static string CsvEscape(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }
}
