using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.DatabaseIntelligence;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence;

public sealed class DbIntelligenceConnectionVault : IDbConnectionVault
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeyVersionHeaderSize = 2;
    private static ReadOnlySpan<byte> VaultMagic => "DBIV"u8;

    private readonly DbIntelligenceSecurityOptions _options;

    public DbIntelligenceConnectionVault(IOptions<DbIntelligenceSecurityOptions> options) => _options = options.Value;

    public byte[] Encrypt(DbConnectionSecrets secrets)
    {
        var plaintext = JsonSerializer.SerializeToUtf8Bytes(secrets);
        var keyId = _options.ActiveEncryptionKeyId;
        var key = ResolveKey(keyId);
        var nonce = RandomNumberGenerator.GetBytes(NonceSize);
        var cipher = new byte[plaintext.Length];
        var tag = new byte[TagSize];
        using var aes = new AesGcm(key, TagSize);
        aes.Encrypt(nonce, plaintext, cipher, tag);

        using var ms = new MemoryStream();
        ms.Write(VaultMagic);
        ms.Write(Encoding.UTF8.GetBytes(keyId.PadRight(KeyVersionHeaderSize)[..KeyVersionHeaderSize]));
        ms.Write(nonce);
        ms.Write(cipher);
        ms.Write(tag);
        return ms.ToArray();
    }

    public DbConnectionSecrets Decrypt(byte[] encryptedBlob)
    {
        if (encryptedBlob.Length < VaultMagic.Length + KeyVersionHeaderSize + NonceSize + TagSize + 1)
            throw new InvalidDataException("Encrypted connection blob is invalid.");

        var offset = 0;
        var magic = encryptedBlob.AsSpan(offset, VaultMagic.Length);
        if (!magic.SequenceEqual(VaultMagic))
            throw new InvalidDataException("Encrypted connection blob has invalid header.");
        offset += VaultMagic.Length;

        var keyId = Encoding.UTF8.GetString(encryptedBlob, offset, KeyVersionHeaderSize).TrimEnd('\0', ' ');
        offset += KeyVersionHeaderSize;

        var nonce = encryptedBlob.AsSpan(offset, NonceSize);
        offset += NonceSize;

        var cipherLength = encryptedBlob.Length - offset - TagSize;
        var cipher = encryptedBlob.AsSpan(offset, cipherLength);
        offset += cipherLength;
        var tag = encryptedBlob.AsSpan(offset, TagSize);

        var key = ResolveKey(keyId);
        var plain = new byte[cipherLength];
        using var aes = new AesGcm(key, TagSize);
        aes.Decrypt(nonce, cipher, tag, plain);

        return JsonSerializer.Deserialize<DbConnectionSecrets>(plain)
               ?? throw new InvalidDataException("Encrypted connection blob could not be deserialized.");
    }

    private byte[] ResolveKey(string keyId)
    {
        if (!_options.EncryptionKeys.TryGetValue(keyId, out var b64) || string.IsNullOrWhiteSpace(b64))
            throw new InvalidOperationException($"Encryption key '{keyId}' is not configured.");

        var key = Convert.FromBase64String(b64);
        return key.Length == 32 ? key : SHA256.HashData(key);
    }
}
