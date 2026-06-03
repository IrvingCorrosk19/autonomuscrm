using System.Security.Cryptography;
using System.Text;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Configuration;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class IntegrationTokenProtector : IIntegrationTokenProtector
{
    private const string Prefix = "enc:v1:";
    private readonly byte[]? _key;

    public IntegrationTokenProtector(IConfiguration configuration)
    {
        var keyB64 = configuration["IntegrationEncryption:Key"];
        if (!string.IsNullOrWhiteSpace(keyB64))
        {
            try
            {
                var key = Convert.FromBase64String(keyB64);
                if (key.Length >= 32)
                    _key = key[..32];
            }
            catch (FormatException)
            {
                // misconfigured key — fall back to plain: prefix at rest
            }
        }
    }

    public bool EncryptionConfigured => _key is not null;

    public string Protect(string? plaintext)
    {
        if (string.IsNullOrEmpty(plaintext)) return plaintext ?? "";
        if (_key is null) return $"plain:{plaintext}";

        var nonce = RandomNumberGenerator.GetBytes(12);
        var plain = Encoding.UTF8.GetBytes(plaintext);
        var cipher = new byte[plain.Length];
        var tag = new byte[16];
        using var aes = new AesGcm(_key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);
        return Prefix + Convert.ToBase64String(nonce) + ":" + Convert.ToBase64String(cipher) + ":" + Convert.ToBase64String(tag);
    }

    public string? Unprotect(string? stored)
    {
        if (string.IsNullOrEmpty(stored)) return stored;
        if (stored.StartsWith("plain:", StringComparison.Ordinal))
            return stored["plain:".Length..];
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal) || _key is null)
            return stored;

        var parts = stored[Prefix.Length..].Split(':');
        if (parts.Length != 3) return stored;
        var nonce = Convert.FromBase64String(parts[0]);
        var cipher = Convert.FromBase64String(parts[1]);
        var tag = Convert.FromBase64String(parts[2]);
        var plain = new byte[cipher.Length];
        using var aes = new AesGcm(_key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return Encoding.UTF8.GetString(plain);
    }
}
