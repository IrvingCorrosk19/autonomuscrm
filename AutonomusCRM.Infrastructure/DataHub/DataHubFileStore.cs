using AutonomusCRM.Application.DataHub;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.DataHub;

public sealed class DataHubFileStore
{
    private readonly string _root;
    private readonly DataHubSecurityOptions _securityOptions;
    private readonly DataHubFileEncryption? _encryption;

    public DataHubFileStore(
        IConfiguration configuration,
        IOptions<DataHubSecurityOptions> securityOptions,
        DataHubFileEncryption? encryption = null)
    {
        _root = configuration["DataHub:StoragePath"]
            ?? Path.Combine(Path.GetTempPath(), "autonomuscrm-datahub");
        _securityOptions = securityOptions.Value;
        _encryption = encryption;
        Directory.CreateDirectory(_root);
    }

    public async Task<string> SaveAsync(Guid tenantId, Guid jobId, Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        var tenantDir = Path.Combine(_root, tenantId.ToString("N"));
        Directory.CreateDirectory(tenantDir);
        var safeName = Path.GetFileName(fileName);
        var path = Path.Combine(tenantDir, $"{jobId:N}_{safeName}.enc");

        if (_securityOptions.EncryptStorage && _encryption != null)
            await _encryption.EncryptToFileAsync(content, path, cancellationToken);
        else
        {
            path = Path.Combine(tenantDir, $"{jobId:N}_{safeName}");
            await using var fs = File.Create(path);
            await content.CopyToAsync(fs, cancellationToken);
        }

        return path;
    }

    public Stream OpenRead(string path)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Import file not found", path);
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal detected");

        if (path.EndsWith(".enc", StringComparison.OrdinalIgnoreCase) && _encryption != null)
            return _encryption.DecryptToTempFileStreamAsync(full).GetAwaiter().GetResult();

        return File.OpenRead(full);
    }

    public void Delete(string path)
    {
        if (!File.Exists(path)) return;
        var full = Path.GetFullPath(path);
        if (!full.StartsWith(Path.GetFullPath(_root), StringComparison.OrdinalIgnoreCase))
            throw new UnauthorizedAccessException("Path traversal detected");
        File.Delete(full);
    }
}
