using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubTenantGuard : IDataHubTenantGuard
{
    private readonly IHttpContextAccessor? _httpContextAccessor;

    public DataHubTenantGuard(IHttpContextAccessor? httpContextAccessor = null)
        => _httpContextAccessor = httpContextAccessor;

    public Guid? GetCurrentTenantId()
    {
        var user = _httpContextAccessor?.HttpContext?.User;
        if (user == null) return null;
        var claim = user.FindFirst("TenantId")?.Value ?? user.FindFirst("tenant_id")?.Value;
        return Guid.TryParse(claim, out var tid) ? tid : null;
    }

    public bool IsSameTenant(Guid requestedTenantId)
    {
        if (requestedTenantId == Guid.Empty) return false;
        var current = GetCurrentTenantId();
        if (current == null) return false;
        return current.Value == requestedTenantId;
    }

    public void EnsureSameTenant(Guid requestedTenantId)
    {
        if (!IsSameTenant(requestedTenantId))
            throw new DataHubTenantAccessException("Cross-tenant access denied.");
    }
}

public sealed class DataHubFileEncryption
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeyVersionHeaderSize = 2;
    private const int StreamChunkSize = 64 * 1024;
    private static ReadOnlySpan<byte> StreamMagic => "DHUB"u8;

    private readonly DataHubSecurityOptions _options;

    public DataHubFileEncryption(IOptions<DataHubSecurityOptions> options) => _options = options.Value;

    public async Task EncryptToFileAsync(Stream plaintext, string path, CancellationToken cancellationToken = default)
    {
        var key = ResolveKey(_options.ActiveEncryptionKeyId);
        await using var output = File.Create(path);
        await output.WriteAsync(StreamMagic.ToArray(), cancellationToken);
        await output.WriteAsync(Encoding.UTF8.GetBytes(_options.ActiveEncryptionKeyId.PadRight(KeyVersionHeaderSize)[..KeyVersionHeaderSize]), cancellationToken);

        var buffer = new byte[StreamChunkSize];
        using var aes = new AesGcm(key, TagSize);
        while (true)
        {
            var read = await plaintext.ReadAsync(buffer.AsMemory(0, StreamChunkSize), cancellationToken);
            if (read == 0)
            {
                await output.WriteAsync(BitConverter.GetBytes(0), cancellationToken);
                break;
            }

            var nonce = RandomNumberGenerator.GetBytes(NonceSize);
            var cipher = new byte[read];
            var tag = new byte[TagSize];
            aes.Encrypt(nonce, buffer.AsSpan(0, read), cipher, tag);
            await output.WriteAsync(BitConverter.GetBytes(read), cancellationToken);
            await output.WriteAsync(nonce, cancellationToken);
            await output.WriteAsync(cipher, cancellationToken);
            await output.WriteAsync(tag, cancellationToken);
        }
    }

    public async Task<Stream> DecryptToMemoryStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        var temp = await DecryptToTempFileStreamAsync(path, cancellationToken);
        var ms = new MemoryStream();
        await temp.CopyToAsync(ms, cancellationToken);
        await temp.DisposeAsync();
        ms.Position = 0;
        return ms;
    }

    public async Task<FileStream> DecryptToTempFileStreamAsync(string path, CancellationToken cancellationToken = default)
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"datahub-dec-{Guid.NewGuid():N}.tmp");
        await using (var input = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamChunkSize, FileOptions.Asynchronous))
        await using (var output = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, StreamChunkSize, FileOptions.Asynchronous))
        {
            var magic = new byte[4];
            await input.ReadExactlyAsync(magic, cancellationToken);
            if (magic.AsSpan().SequenceEqual(StreamMagic))
                await DecryptStreamingChunksAsync(input, output, cancellationToken);
            else
            {
                input.Position = 0;
                await DecryptLegacyToStreamAsync(input, output, cancellationToken);
            }
        }

        return new FileStream(tempPath, FileMode.Open, FileAccess.Read, FileShare.Read, StreamChunkSize,
            FileOptions.DeleteOnClose | FileOptions.Asynchronous);
    }

    private async Task DecryptStreamingChunksAsync(Stream input, Stream output, CancellationToken cancellationToken)
    {
        var keyIdBytes = new byte[KeyVersionHeaderSize];
        await input.ReadExactlyAsync(keyIdBytes, cancellationToken);
        var keyId = Encoding.UTF8.GetString(keyIdBytes).TrimEnd('\0', ' ');
        var key = ResolveKey(keyId);
        using var aes = new AesGcm(key, TagSize);
        var lenBuf = new byte[4];
        var nonce = new byte[NonceSize];
        while (true)
        {
            await input.ReadExactlyAsync(lenBuf, cancellationToken);
            var chunkLen = BitConverter.ToInt32(lenBuf);
            if (chunkLen == 0) break;
            await input.ReadExactlyAsync(nonce, cancellationToken);
            var cipher = new byte[chunkLen];
            var tag = new byte[TagSize];
            await input.ReadExactlyAsync(cipher, cancellationToken);
            await input.ReadExactlyAsync(tag, cancellationToken);
            var plain = new byte[chunkLen];
            aes.Decrypt(nonce, cipher, tag, plain);
            await output.WriteAsync(plain, cancellationToken);
        }
    }

    private async Task DecryptLegacyToStreamAsync(Stream input, Stream output, CancellationToken cancellationToken)
    {
        var header = new byte[KeyVersionHeaderSize + NonceSize];
        await input.ReadExactlyAsync(header, cancellationToken);
        using var ms = new MemoryStream();
        await input.CopyToAsync(ms, cancellationToken);
        var bytes = ms.ToArray();
        if (bytes.Length < TagSize) throw new InvalidDataException("Encrypted file is too short.");
        var keyId = Encoding.UTF8.GetString(header, 0, KeyVersionHeaderSize).TrimEnd('\0', ' ');
        var key = ResolveKey(keyId);
        var tag = bytes.AsSpan(bytes.Length - TagSize, TagSize);
        var cipher = bytes.AsSpan(0, bytes.Length - TagSize);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(header.AsSpan(KeyVersionHeaderSize, NonceSize), cipher, tag, plain);
        await output.WriteAsync(plain, cancellationToken);
    }

    private byte[] ResolveKey(string keyId)
        => ResolveKeyStatic(keyId, _options);

    private static byte[] ResolveKeyStatic(string keyId, DataHubSecurityOptions? options)
    {
        if (options == null)
            throw new InvalidOperationException("Encryption options required.");
        if (!options.EncryptionKeys.TryGetValue(keyId, out var b64) || string.IsNullOrWhiteSpace(b64))
            throw new InvalidOperationException($"Encryption key '{keyId}' is not configured.");
        var key = Convert.FromBase64String(b64);
        return key.Length == 32 ? key : SHA256.HashData(key);
    }
}

public sealed class HeuristicMalwareScanner : IDataHubMalwareScanner
{
    private static readonly byte[] Eicar = Encoding.ASCII.GetBytes(
        "X5O!P%@AP[4\\PZX54(P^)7CC)7}$EICAR-STANDARD-ANTIVIRUS-TEST-FILE!$H+H*");

    public async Task<DataHubMalwareScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (content is FileStream fs && fs.CanSeek)
            return await ScanSeekableStreamAsync(fs, fileName, cancellationToken);

        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);
        buffer.Position = 0;
        return await ScanSeekableStreamAsync(buffer, fileName, cancellationToken);
    }

    private static async Task<DataHubMalwareScanResult> ScanSeekableStreamAsync(
        Stream stream, string fileName, CancellationToken cancellationToken)
    {
        const int chunkSize = 64 * 1024;
        var buffer = new byte[chunkSize];
        var carry = Array.Empty<byte>();
        var scriptWindow = new StringBuilder();
        long totalBytes = 0;

        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, chunkSize), cancellationToken);
            if (read == 0) break;
            totalBytes += read;

            var chunk = buffer.AsSpan(0, read);
            if (ContainsSequence(Combine(carry, chunk), Eicar))
                return new DataHubMalwareScanResult(false, "EICAR-Test-File", "Heuristic");

            scriptWindow.Append(Encoding.UTF8.GetString(chunk));
            if (scriptWindow.Length > chunkSize * 2)
                scriptWindow.Remove(0, scriptWindow.Length - chunkSize);

            var text = scriptWindow.ToString();
            if (text.Contains("<?php", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("<script", StringComparison.OrdinalIgnoreCase) ||
                text.Contains("powershell -", StringComparison.OrdinalIgnoreCase))
                return new DataHubMalwareScanResult(false, "SuspiciousScriptContent", "Heuristic");

            carry = chunk.Length >= Eicar.Length - 1
                ? chunk.Slice(chunk.Length - (Eicar.Length - 1)).ToArray()
                : chunk.ToArray();
        }

        if (Path.GetExtension(fileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase) &&
            totalBytes > 4)
        {
            stream.Position = 0;
            var header = new byte[4];
            await stream.ReadExactlyAsync(header, cancellationToken);
            if (header[0] != 0x50 || header[1] != 0x4B)
                return new DataHubMalwareScanResult(false, "CorruptedXlsx", "Heuristic");
        }

        return new DataHubMalwareScanResult(true, null, "Heuristic");
    }

    private static byte[] Combine(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
    {
        var combined = new byte[left.Length + right.Length];
        left.CopyTo(combined);
        right.CopyTo(combined.AsSpan(left.Length));
        return combined;
    }

    private static bool ContainsSequence(ReadOnlySpan<byte> haystack, ReadOnlySpan<byte> needle)
    {
        return haystack.IndexOf(needle) >= 0;
    }
}

public sealed class ClamAvMalwareScanner : IDataHubMalwareScanner
{
    private readonly DataHubSecurityOptions _options;
    private readonly IDataHubMalwareScanner _fallback;
    private readonly ILogger<ClamAvMalwareScanner> _logger;

    public ClamAvMalwareScanner(
        IOptions<DataHubSecurityOptions> options,
        HeuristicMalwareScanner fallback,
        ILogger<ClamAvMalwareScanner> logger)
    {
        _options = options.Value;
        _fallback = fallback;
        _logger = logger;
    }

    public async Task<DataHubMalwareScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ClamAvHost))
            return await _fallback.ScanAsync(content, fileName, cancellationToken);

        if (content.CanSeek)
            content.Position = 0;

        try
        {
            using var client = new TcpClient();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromSeconds(_options.ClamAvTimeoutSeconds));
            await client.ConnectAsync(_options.ClamAvHost, _options.ClamAvPort, cts.Token);
            await using var stream = client.GetStream();
            await stream.WriteAsync("zINSTREAM\0"u8.ToArray(), cts.Token);

            var buffer = new byte[2048];
            while (true)
            {
                var read = await content.ReadAsync(buffer.AsMemory(0, buffer.Length), cts.Token);
                if (read == 0) break;
                var sizeBytes = BitConverter.GetBytes(System.Net.IPAddress.HostToNetworkOrder(read));
                await stream.WriteAsync(sizeBytes, cts.Token);
                await stream.WriteAsync(buffer.AsMemory(0, read), cts.Token);
            }

            await stream.WriteAsync(BitConverter.GetBytes(0), cts.Token);
            using var reader = new StreamReader(stream, Encoding.ASCII);
            var response = await reader.ReadLineAsync(cts.Token) ?? "";
            if (response.EndsWith("OK", StringComparison.OrdinalIgnoreCase))
                return new DataHubMalwareScanResult(true, null, "ClamAV");
            if (response.Contains("FOUND", StringComparison.OrdinalIgnoreCase))
            {
                var threat = response.Split(':').LastOrDefault()?.Replace(" FOUND", "").Trim() ?? "Unknown";
                return new DataHubMalwareScanResult(false, threat, "ClamAV");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "ClamAV unavailable, falling back to heuristic scan");
            if (content.CanSeek) content.Position = 0;
            return await _fallback.ScanAsync(content, fileName, cancellationToken);
        }

        if (content.CanSeek) content.Position = 0;
        return await _fallback.ScanAsync(content, fileName, cancellationToken);
    }
}

public sealed class DataHubForensicAuditService : IDataHubForensicAuditService
{
    private readonly ApplicationDbContext _db;

    public DataHubForensicAuditService(ApplicationDbContext db) => _db = db;

    public async Task RecordAsync(DataHubForensicAuditEntry entry, CancellationToken cancellationToken = default)
    {
        _db.DataHubForensicAudits.Add(new DataHubForensicAudit
        {
            Id = Guid.NewGuid(),
            TenantId = entry.TenantId,
            UserId = entry.UserId,
            JobId = entry.JobId,
            Action = entry.Action,
            FileName = entry.FileName,
            FileSizeBytes = entry.FileSizeBytes,
            FileHashSha256 = entry.FileHashSha256,
            IpAddress = entry.IpAddress,
            UserAgent = entry.UserAgent,
            Details = entry.Details,
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    public Task<IReadOnlyList<DataHubForensicAudit>> GetRecentAsync(Guid tenantId, int take = 100, CancellationToken cancellationToken = default)
        => _db.DataHubForensicAudits
            .Where(a => a.TenantId == tenantId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken)
            .ContinueWith(t => (IReadOnlyList<DataHubForensicAudit>)t.Result, cancellationToken);

    public Task<int> CountActionsAsync(Guid tenantId, string action, TimeSpan window, CancellationToken cancellationToken = default)
    {
        var since = DateTime.UtcNow - window;
        return _db.DataHubForensicAudits
            .CountAsync(a => a.TenantId == tenantId && a.Action == action && a.CreatedAt >= since, cancellationToken);
    }
}

public sealed class DataHubSecurityQuotaService : IDataHubSecurityQuotaService
{
    private readonly DataHubSecurityOptions _options;
    private readonly IDataHubRepository _repo;
    private readonly IDataHubForensicAuditService _audit;
    private static readonly ConcurrentDictionary<Guid, SemaphoreSlim> TenantLocks = new();

    public DataHubSecurityQuotaService(
        IOptions<DataHubSecurityOptions> options,
        IDataHubRepository repo,
        IDataHubForensicAuditService audit)
    {
        _options = options.Value;
        _repo = repo;
        _audit = audit;
    }

    public async Task EnsureUploadAllowedAsync(Guid tenantId, long fileSizeBytes, CancellationToken cancellationToken = default)
    {
        if (fileSizeBytes > _options.MaxFileBytes)
            throw new DataHubSecurityQuotaException($"File exceeds maximum size of {_options.MaxFileBytes / 1024 / 1024} MB.");

        var imports = await _audit.CountActionsAsync(tenantId, DataHubForensicActions.Upload, TimeSpan.FromHours(1), cancellationToken);
        if (imports >= _options.MaxImportsPerHour)
            throw new DataHubSecurityQuotaException($"Import quota exceeded ({_options.MaxImportsPerHour}/hour).");

        await EnsureConcurrentJobsAsync(tenantId, cancellationToken);
    }

    public async Task EnsureExportAllowedAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var exports = await _audit.CountActionsAsync(tenantId, DataHubForensicActions.Export, TimeSpan.FromHours(1), cancellationToken);
        if (exports >= _options.MaxExportsPerHour)
            throw new DataHubSecurityQuotaException($"Export quota exceeded ({_options.MaxExportsPerHour}/hour).");
    }

    public async Task EnsureImportStartAllowedAsync(Guid tenantId, CancellationToken cancellationToken = default)
        => await EnsureConcurrentJobsAsync(tenantId, cancellationToken);

    private async Task EnsureConcurrentJobsAsync(Guid tenantId, CancellationToken cancellationToken)
    {
        var gate = TenantLocks.GetOrAdd(tenantId, _ => new SemaphoreSlim(1, 1));
        await gate.WaitAsync(cancellationToken);
        try
        {
            var active = await _repo.CountActiveJobsAsync(tenantId, cancellationToken);
            if (active >= _options.MaxConcurrentJobs)
                throw new DataHubSecurityQuotaException($"Concurrent job limit reached ({_options.MaxConcurrentJobs}).");
        }
        finally
        {
            gate.Release();
        }
    }
}

public static class DataHubSecurityContext
{
    public static (string? Ip, string? UserAgent) FromHttp(HttpContext? ctx)
    {
        if (ctx == null) return (null, null);
        var ip = ctx.Connection.RemoteIpAddress?.ToString();
        var ua = ctx.Request.Headers.UserAgent.ToString();
        return (ip, string.IsNullOrEmpty(ua) ? null : ua);
    }

    public static string ComputeSha256(Stream content)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(content);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

public sealed class NullDataHubRequestContext : IDataHubRequestContext
{
    public string? ClientIp => null;
    public string? UserAgent => null;
}
