using System.Globalization;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using AutonomusCRM.Application.DataHub;
using ClosedXML.Excel;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubExtractService : IDataHubExtractService
{
    public async Task<(List<string> Columns, List<Dictionary<string, string?>> Rows, string Encoding, string? Delimiter)> ExtractAsync(
        Stream stream, string fileName, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        return ext switch
        {
            ".json" => await ExtractJsonAsync(stream, cancellationToken),
            ".xlsx" or ".xls" => ExtractExcel(stream),
            ".txt" => ExtractDelimited(await ReadTextAsync(stream, cancellationToken), DetectDelimiter(await PeekTextAsync(stream, cancellationToken))),
            _ => ExtractDelimited(await ReadTextAsync(stream, cancellationToken), DetectDelimiter(await PeekTextAsync(stream, cancellationToken)))
        };
    }

    public async IAsyncEnumerable<DataHubExtractChunk> ExtractInChunksAsync(
        Stream stream, string fileName, int chunkSize = DataHubConstants.ExtractChunkSize,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (ext is not (".csv" or ".txt"))
        {
            var full = await ExtractAsync(stream, fileName, cancellationToken);
            yield return new DataHubExtractChunk(full.Columns, full.Rows, 1, full.Encoding, full.Delimiter, true);
            yield break;
        }

        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var firstLine = await reader.ReadLineAsync(cancellationToken);
        if (firstLine == null) yield break;

        var delimiter = DetectDelimiter(firstLine);
        var parsedFirst = ParseCsvLine(firstLine, delimiter);
        var columns = parsedFirst.Select(NormalizeHeader).ToList();
        var hasHeader = columns.Any(c => !string.IsNullOrWhiteSpace(c) && !char.IsDigit(c.FirstOrDefault()));
        List<string>? pendingFirstRow = null;

        if (!hasHeader)
        {
            columns = parsedFirst.Select((_, i) => $"Column{i + 1}").ToList();
            pendingFirstRow = parsedFirst;
        }

        var buffer = new List<Dictionary<string, string?>>();
        var nextRowNum = 1;
        var isFirst = true;

        while (true)
        {
            List<string> fields;
            if (pendingFirstRow != null)
            {
                fields = pendingFirstRow;
                pendingFirstRow = null;
            }
            else
            {
                var line = await reader.ReadLineAsync(cancellationToken);
                if (line == null)
                {
                    if (buffer.Count > 0)
                        yield return new DataHubExtractChunk(columns, buffer.ToList(), nextRowNum - buffer.Count, "UTF-8", delimiter, isFirst);
                    yield break;
                }
                fields = ParseCsvLine(line, delimiter);
            }

            if (fields.All(string.IsNullOrWhiteSpace)) continue;

            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < columns.Count; c++)
                dict[columns[c]] = c < fields.Count ? fields[c]?.Trim() : null;
            buffer.Add(dict);
            nextRowNum++;

            if (buffer.Count >= chunkSize)
            {
                yield return new DataHubExtractChunk(columns, buffer.ToList(), nextRowNum - buffer.Count, "UTF-8", delimiter, isFirst);
                isFirst = false;
                buffer.Clear();
            }
        }
    }

    private static async Task<(List<string>, List<Dictionary<string, string?>>, string, string?)> ExtractJsonAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        var content = await reader.ReadToEndAsync(cancellationToken);
        var doc = JsonSerializer.Deserialize<List<Dictionary<string, JsonElement>>>(content,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? new();

        var columns = doc.SelectMany(d => d.Keys).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
        var rows = doc.Select(d =>
        {
            var row = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            foreach (var col in columns)
            {
                if (d.TryGetValue(col, out var el))
                    row[col] = JsonElementToString(el);
            }
            return row;
        }).ToList();

        return (columns, rows, "UTF-8", null);
    }

    private static (List<string>, List<Dictionary<string, string?>>, string, string?) ExtractExcel(Stream stream)
    {
        using var workbook = new XLWorkbook(stream);
        var sheet = workbook.Worksheets.First();
        var used = sheet.RangeUsed();
        if (used == null) return (new List<string>(), new List<Dictionary<string, string?>>(), "UTF-8", null);

        var firstRow = used.FirstRow();
        var columns = firstRow.Cells().Select(c => c.GetString().Trim()).Where(c => !string.IsNullOrWhiteSpace(c)).ToList();
        var rows = new List<Dictionary<string, string?>>();

        foreach (var row in used.RowsUsed().Skip(1))
        {
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < columns.Count; i++)
            {
                var cell = row.Cell(i + 1);
                dict[columns[i]] = cell.IsEmpty() ? null : cell.GetFormattedString().Trim();
            }
            if (dict.Values.Any(v => !string.IsNullOrWhiteSpace(v)))
                rows.Add(dict);
        }

        return (columns, rows, "UTF-8", null);
    }

    private static (List<string>, List<Dictionary<string, string?>>, string, string?) ExtractDelimited(string content, string delimiter)
    {
        var lines = SplitLines(content);
        if (lines.Count == 0) return (new List<string>(), new List<Dictionary<string, string?>>(), "UTF-8", delimiter);

        var headerLine = lines[0];
        var columns = ParseCsvLine(headerLine, delimiter).Select(NormalizeHeader).ToList();
        var hasHeader = columns.Any(c => !string.IsNullOrWhiteSpace(c) && !char.IsDigit(c.FirstOrDefault()));
        if (!hasHeader)
        {
            columns = Enumerable.Range(1, ParseCsvLine(headerLine, delimiter).Count).Select(i => $"Column{i}").ToList();
        }

        var start = hasHeader ? 1 : 0;
        var rows = new List<Dictionary<string, string?>>();
        for (var i = start; i < lines.Count; i++)
        {
            var fields = ParseCsvLine(lines[i], delimiter);
            if (fields.All(string.IsNullOrWhiteSpace)) continue;
            var dict = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            for (var c = 0; c < columns.Count; c++)
                dict[columns[c]] = c < fields.Count ? fields[c]?.Trim() : null;
            rows.Add(dict);
        }

        return (columns, rows, "UTF-8", delimiter);
    }

    public static List<string> ParseCsvLine(string line, string delimiter)
    {
        var result = new List<string>();
        var sb = new StringBuilder();
        var inQuotes = false;
        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];
            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    sb.Append('"');
                    i++;
                }
                else inQuotes = !inQuotes;
            }
            else if (!inQuotes && line.AsSpan(i).StartsWith(delimiter))
            {
                result.Add(sb.ToString());
                sb.Clear();
                i += delimiter.Length - 1;
            }
            else sb.Append(ch);
        }
        result.Add(sb.ToString());
        return result;
    }

    public static string DetectDelimiter(string sample)
    {
        var firstLine = sample.Split('\n').FirstOrDefault() ?? "";
        var counts = new Dictionary<string, int>
        {
            [","] = firstLine.Count(c => c == ','),
            [";"] = firstLine.Count(c => c == ';'),
            ["\t"] = firstLine.Count(c => c == '\t'),
            ["|"] = firstLine.Count(c => c == '|')
        };
        return counts.OrderByDescending(kv => kv.Value).First().Key;
    }

    private static async Task<string> ReadTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private static async Task<string> PeekTextAsync(Stream stream, CancellationToken cancellationToken)
    {
        stream.Position = 0;
        var buffer = new byte[4096];
        var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken);
        stream.Position = 0;
        return Encoding.UTF8.GetString(buffer, 0, read);
    }

    private static List<string> SplitLines(string content)
        => content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None).Where(l => l.Length > 0).ToList();

    private static string NormalizeHeader(string header)
        => Regex.Replace(header.Trim(), @"\s+", " ");

    private static string? JsonElementToString(JsonElement el) => el.ValueKind switch
    {
        JsonValueKind.Null or JsonValueKind.Undefined => null,
        JsonValueKind.String => el.GetString(),
        JsonValueKind.Number => el.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        _ => el.GetRawText()
    };
}
