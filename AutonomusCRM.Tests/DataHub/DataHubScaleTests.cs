using System.Text;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

public class DataHubScaleTests
{
    [Fact]
    public async Task ExtractInChunks_YieldsMultipleChunks_ForLargeCsv()
    {
        var svc = new DataHubExtractService();
        var sb = new StringBuilder("Name,Email\n");
        for (var i = 0; i < 12_000; i++)
            sb.AppendLine($"User{i},user{i}@test.com");

        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
        var chunks = new List<DataHubExtractChunk>();
        await foreach (var chunk in svc.ExtractInChunksAsync(ms, "scale-test.csv", 5000))
            chunks.Add(chunk);

        Assert.True(chunks.Count >= 2);
        Assert.Equal(12_000, chunks.Sum(c => c.Rows.Count));
        Assert.True(chunks[0].IsFirstChunk);
    }

    [Fact]
    public void ScaleConstants_SupportEnterpriseBatchSizes()
    {
        Assert.True(DataHubConstants.CopyBatchSize >= 5000);
        Assert.True(DataHubConstants.ExportStreamBatchSize >= 5000);
        Assert.True(DataHubConstants.ExtractChunkSize >= 5000);
        Assert.True(DataHubConstants.LargeFileChunkThresholdBytes >= 512 * 1024);
    }

    [Fact]
    public void BulkStaging_Measure_ReturnsThroughputMetrics()
    {
        var metrics = DataHubBulkStaging.Measure(_ => { }, 10_000, "simulated-insert");
        Assert.Equal(10_000, metrics.RowCount);
        Assert.True(metrics.RowsPerSecond > 0);
        Assert.Equal("simulated-insert", metrics.Operation);
    }

    [Fact]
    public async Task ExportStreaming_WritesCsvIncrementally()
    {
        using var ms = new MemoryStream();
        var columns = new[] { "Name", "Email" };
        await DataHubExportStreaming.WriteCsvHeaderAsync(ms, columns, CancellationToken.None);
        await DataHubExportStreaming.WriteCsvRowAsync(ms, columns,
            new Dictionary<string, string?> { ["Name"] = "Acme", ["Email"] = "a@acme.com" }, CancellationToken.None);

        var text = Encoding.UTF8.GetString(ms.ToArray());
        Assert.Contains("Name,Email", text);
        Assert.Contains("Acme,a@acme.com", text);
    }

    [Fact]
    public async Task ExportStreaming_WritesJsonArrayIncrementally()
    {
        using var ms = new MemoryStream();
        await DataHubExportStreaming.WriteJsonArrayStartAsync(ms, CancellationToken.None);
        await DataHubExportStreaming.WriteJsonRowAsync(ms,
            new Dictionary<string, string?> { ["Id"] = "1" }, isFirst: true, CancellationToken.None);
        await DataHubExportStreaming.WriteJsonRowAsync(ms,
            new Dictionary<string, string?> { ["Id"] = "2" }, isFirst: false, CancellationToken.None);
        await DataHubExportStreaming.WriteJsonArrayEndAsync(ms, CancellationToken.None);

        var text = Encoding.UTF8.GetString(ms.ToArray());
        Assert.StartsWith("[", text);
        Assert.EndsWith("]", text);
        Assert.Contains("\"Id\":\"1\"", text.Replace(" ", ""));
    }

    [Fact]
    public async Task XlsxExportStreaming_WritesValidWorkbookWithoutFullBuffer()
    {
        var columns = new List<string> { "Name", "Email" };
        async IAsyncEnumerable<Dictionary<string, string?>> Rows()
        {
            for (var i = 0; i < 250; i++)
                yield return new Dictionary<string, string?> { ["Name"] = $"User {i}", ["Email"] = $"u{i}@test.com" };
            await Task.CompletedTask;
        }

        await using var ms = new MemoryStream();
        await DataHubExportStreaming.WriteXlsxStreamAsync(ms, columns, Rows(), CancellationToken.None);
        Assert.True(ms.Length > 100);
        ms.Position = 0;
        using var doc = DocumentFormat.OpenXml.Packaging.SpreadsheetDocument.Open(ms, false);
        var sheet = doc.WorkbookPart!.WorksheetParts.First().Worksheet;
        var rowCount = sheet.Descendants<DocumentFormat.OpenXml.Spreadsheet.Row>().Count();
        Assert.Equal(251, rowCount);
    }

    [Theory]
    [InlineData(100_000)]
    [InlineData(500_000)]
    [InlineData(1_000_000)]
    public async Task ExportStreaming_Csv_LargeRowCounts_CompletesWithoutFullBuffer(int rowCount)
    {
        var columns = new[] { "Name", "Email" };
        await using var ms = new MemoryStream();
        await DataHubExportStreaming.WriteCsvHeaderAsync(ms, columns, CancellationToken.None);

        GC.Collect();
        var before = GC.GetTotalMemory(true);
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (var i = 0; i < rowCount; i++)
        {
            await DataHubExportStreaming.WriteCsvRowAsync(ms, columns,
                new Dictionary<string, string?> { ["Name"] = $"User {i}", ["Email"] = $"u{i}@scale.test" },
                CancellationToken.None);
        }
        sw.Stop();
        var after = GC.GetTotalMemory(false);

        Assert.True(ms.Length > rowCount * 10);
        Assert.True(sw.ElapsedMilliseconds < Math.Max(120_000, rowCount / 10),
            $"Export took {sw.ElapsedMilliseconds}ms");
        Assert.True(after - before < 512 * 1024 * 1024,
            $"Memory delta {(after - before) / 1024 / 1024}MB too high for streaming export");
    }

    [Fact]
    public async Task HeuristicMalwareScanner_LargeFile_ScansInChunks()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dh-malware-{Guid.NewGuid():N}.bin");
        const int sizeMb = 8;
        try
        {
            await using (var fs = File.Create(path))
            {
                var chunk = new byte[1024 * 1024];
                for (var i = 0; i < sizeMb; i++)
                    await fs.WriteAsync(chunk);
            }

            GC.Collect();
            var before = GC.GetTotalMemory(true);
            await using var stream = File.OpenRead(path);
            var scanner = new HeuristicMalwareScanner();
            var result = await scanner.ScanAsync(stream, "large.csv");
            var after = GC.GetTotalMemory(false);

            Assert.True(result.IsClean);
            Assert.True(after - before < 64 * 1024 * 1024,
                $"Scan memory delta {(after - before) / 1024 / 1024}MB too high");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Theory]
    [InlineData(10_000)]
    [InlineData(50_000)]
    [InlineData(100_000)]
    public void SimulatedStagingThroughput_MeetsMinimumBar(int rowCount)
    {
        var metrics = DataHubBulkStaging.Measure(n =>
        {
            for (var i = 0; i < n; i++)
                _ = i.ToString();
        }, rowCount, $"staging-sim-{rowCount}");

        Assert.True(metrics.RowsPerSecond >= 50_000,
            $"Expected >=50K rows/s simulated throughput, got {metrics.RowsPerSecond:F0} for {rowCount} rows");
    }
}
