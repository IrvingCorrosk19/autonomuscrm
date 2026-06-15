using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using AutonomusCRM.Infrastructure.DataHub.Migration;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

public class DataHubFinalRecoveryUnitTests
{
    [Fact]
    public void MigrationQualityGate_BlocksMissingOwners()
    {
        var errors = new List<DataHubImportError>
        {
            new()
            {
                ErrorCode = "MissingOwner",
                FieldName = "OwnerId",
                Message = "Owner missing"
            }
        };
        var dupes = new DataHubDuplicateScanResultDto(Guid.Empty, 0, 0, Array.Empty<DataHubDuplicateGroupDto>());
        var result = MigrationQualityGate.Evaluate(errors, dupes);
        Assert.False(result.Passed);
        Assert.True(result.MissingOwners > 0);
    }

    [Fact]
    public async Task MalwareScanner_DetectsScriptAfter8KbBoundary()
    {
        var scanner = new HeuristicMalwareScanner();
        await using var stream = new MemoryStream();
        var padding = new byte[9000];
        Array.Fill(padding, (byte)'A');
        await stream.WriteAsync(padding);
        await stream.WriteAsync(System.Text.Encoding.UTF8.GetBytes("<?php evil();"));
        stream.Position = 0;

        var result = await scanner.ScanAsync(stream, "payload.csv");
        Assert.False(result.IsClean);
    }
}
