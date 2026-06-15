using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.DataHub;
using Microsoft.AspNetCore.Http;
using Moq;
using Xunit;

namespace AutonomusCRM.Tests.DataHub;

public class DataHubTenantGuardTests
{
    [Fact]
    public void IsSameTenant_RejectsCrossTenant_NoAdminBypass()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim("TenantId", tenantA.ToString()),
                new Claim(ClaimTypes.Role, "Admin")
            ], "test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(ctx);
        var guard = new DataHubTenantGuard(accessor.Object);

        Assert.True(guard.IsSameTenant(tenantA));
        Assert.False(guard.IsSameTenant(tenantB));
        Assert.Throws<DataHubTenantAccessException>(() => guard.EnsureSameTenant(tenantB));
    }

    [Fact]
    public void IsSameTenant_DeniesWhenTenantClaimMissing_FailClosed()
    {
        var ctx = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.Role, "Manager")
            ], "test"))
        };
        var accessor = new Mock<IHttpContextAccessor>();
        accessor.Setup(a => a.HttpContext).Returns(ctx);
        var guard = new DataHubTenantGuard(accessor.Object);

        Assert.False(guard.IsSameTenant(Guid.NewGuid()));
        Assert.Throws<DataHubTenantAccessException>(() => guard.EnsureSameTenant(Guid.NewGuid()));
    }
}

public class DataHubFileEncryptionTests
{
    [Fact]
    public async Task EncryptDecrypt_RoundTrip_PreservesContent()
    {
        var options = Microsoft.Extensions.Options.Options.Create(new DataHubSecurityOptions
        {
            ActiveEncryptionKeyId = "v1",
            EncryptionKeys = new Dictionary<string, string>
            {
                ["v1"] = Convert.ToBase64String(new byte[32])
            }
        });
        var crypto = new DataHubFileEncryption(options);
        var plain = Encoding.UTF8.GetBytes("Name,Email\nJohn,j@x.com\n");
        var path = Path.Combine(Path.GetTempPath(), $"dh-enc-{Guid.NewGuid():N}.enc");

        try
        {
            await using (var input = new MemoryStream(plain))
                await crypto.EncryptToFileAsync(input, path);

            await using var decrypted = await crypto.DecryptToMemoryStreamAsync(path);
            var result = new MemoryStream();
            await decrypted.CopyToAsync(result);
            Assert.Equal(plain, result.ToArray());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public async Task Encryption_KeyRotation_V1AndV2_AreInteroperable()
    {
        var key1 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var key2 = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var plain = Encoding.UTF8.GetBytes("Name,Email\nRotate,rotate@test.com\n");
        var pathV1 = Path.Combine(Path.GetTempPath(), $"dh-v1-{Guid.NewGuid():N}.enc");
        var pathV2 = Path.Combine(Path.GetTempPath(), $"dh-v2-{Guid.NewGuid():N}.enc");

        try
        {
            var cryptoV1 = new DataHubFileEncryption(Microsoft.Extensions.Options.Options.Create(new DataHubSecurityOptions
            {
                ActiveEncryptionKeyId = "v1",
                EncryptionKeys = new Dictionary<string, string> { ["v1"] = key1, ["v2"] = key2 }
            }));
            await using (var input = new MemoryStream(plain))
                await cryptoV1.EncryptToFileAsync(input, pathV1);

            await using (var decryptedV1 = await cryptoV1.DecryptToMemoryStreamAsync(pathV1))
            {
                var roundTrip = new MemoryStream();
                await decryptedV1.CopyToAsync(roundTrip);
                Assert.Equal(plain, roundTrip.ToArray());
            }

            var cryptoV2 = new DataHubFileEncryption(Microsoft.Extensions.Options.Options.Create(new DataHubSecurityOptions
            {
                ActiveEncryptionKeyId = "v2",
                EncryptionKeys = new Dictionary<string, string> { ["v1"] = key1, ["v2"] = key2 }
            }));
            await using (var decryptedForReencrypt = await cryptoV2.DecryptToMemoryStreamAsync(pathV1))
                await cryptoV2.EncryptToFileAsync(decryptedForReencrypt, pathV2);

            await using (var decryptedV2 = await cryptoV1.DecryptToMemoryStreamAsync(pathV2))
            {
                var result = new MemoryStream();
                await decryptedV2.CopyToAsync(result);
                Assert.Equal(plain, result.ToArray());
            }
        }
        finally
        {
            if (File.Exists(pathV1)) File.Delete(pathV1);
            if (File.Exists(pathV2)) File.Delete(pathV2);
        }
    }
}

public class DataHubMalwareScannerTests
{
    [Fact]
    public async Task HeuristicScanner_BlocksEicarTestFile()
    {
        var scanner = new HeuristicMalwareScanner();
        var eicar = Encoding.ASCII.GetBytes(
            "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");
        var result = await scanner.ScanAsync(new MemoryStream(eicar), "test.csv");
        Assert.False(result.IsClean);
        Assert.Contains("EICAR", result.ThreatName ?? "");
    }

    [Fact]
    public async Task HeuristicScanner_AllowsCleanCsv()
    {
        var scanner = new HeuristicMalwareScanner();
        var csv = Encoding.UTF8.GetBytes("Name,Email\nAcme,a@acme.com\n");
        var result = await scanner.ScanAsync(new MemoryStream(csv), "clean.csv");
        Assert.True(result.IsClean);
    }

    [Fact]
    public async Task HeuristicScanner_BlocksCorruptedXlsx()
    {
        var scanner = new HeuristicMalwareScanner();
        var bad = Encoding.UTF8.GetBytes("not-a-real-xlsx");
        var result = await scanner.ScanAsync(new MemoryStream(bad), "bad.xlsx");
        Assert.False(result.IsClean);
    }
}

public class DataHubUploadSecurityTests
{
    [Fact]
    public void ValidateUpload_RejectsPathTraversalAndOversize()
    {
        var svc = new DataHubSecurityService();
        var badName = svc.ValidateUpload("../secrets.csv", 100, "text/csv");
        Assert.False(badName.Ok);

        var oversized = svc.ValidateUpload("big.csv", DataHubConstants.MaxFileBytes + 1, "text/csv");
        Assert.False(oversized.Ok);
    }

    [Fact]
    public void SanitizeCellValue_BlocksFormulaInjection()
    {
        var svc = new DataHubSecurityService();
        Assert.StartsWith("'", svc.SanitizeCellValue("=cmd|'/c calc'!A0"));
        Assert.StartsWith("'", svc.SanitizeCellValue("+1234"));
    }
}

public class DataHubSecurityContextTests
{
    [Fact]
    public void ComputeSha256_IsDeterministic()
    {
        var data = Encoding.UTF8.GetBytes("tenant-isolation-test");
        var hash1 = DataHubSecurityContext.ComputeSha256(new MemoryStream(data));
        var hash2 = DataHubSecurityContext.ComputeSha256(new MemoryStream(data));
        Assert.Equal(hash1, hash2);
        Assert.Equal(64, hash1.Length);
    }
}
