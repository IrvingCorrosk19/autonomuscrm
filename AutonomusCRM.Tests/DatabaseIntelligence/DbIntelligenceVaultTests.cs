using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.Infrastructure.DatabaseIntelligence;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Tests.DatabaseIntelligence;

[Trait("Category", "DatabaseIntelligence")]
public class DbIntelligenceVaultTests
{
    private static DbIntelligenceConnectionVault CreateVault(string keyId = "v1")
    {
        var options = Options.Create(new DbIntelligenceSecurityOptions
        {
            ActiveEncryptionKeyId = keyId,
            EncryptionKeys = new Dictionary<string, string>
            {
                ["v1"] = "QXV0b25vbXVzQ1JNLURhdGFIdWItQUVTMjU2LUtleSEh",
                ["v2"] = "QXV0b25vbXVzQ1JNLURhdGFIdWItVjItS2V5ISE="
            }
        });
        return new DbIntelligenceConnectionVault(options);
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_PreservesPassword()
    {
        var vault = CreateVault();
        var original = new DbConnectionSecrets("SuperSecret!123");
        var encrypted = vault.Encrypt(original);
        var decrypted = vault.Decrypt(encrypted);
        Assert.Equal(original.Password, decrypted.Password);
        Assert.DoesNotContain(original.Password, System.Text.Encoding.UTF8.GetString(encrypted));
    }

    [Fact]
    public void Encrypt_UsesDifferentNonceEachTime()
    {
        var vault = CreateVault();
        var secrets = new DbConnectionSecrets("SamePassword");
        var a = vault.Encrypt(secrets);
        var b = vault.Encrypt(secrets);
        Assert.NotEqual(Convert.ToBase64String(a), Convert.ToBase64String(b));
    }

    [Fact]
    public void Decrypt_WithSecondaryKeyRing_WorksAfterReencrypt()
    {
        var vaultV1 = CreateVault("v1");
        var vaultV2 = CreateVault("v2");
        var encryptedV1 = vaultV1.Encrypt(new DbConnectionSecrets("rotate-me"));
        Assert.Equal("rotate-me", vaultV1.Decrypt(encryptedV1).Password);

        var reencrypted = vaultV2.Encrypt(vaultV1.Decrypt(encryptedV1));
        Assert.Equal("rotate-me", vaultV2.Decrypt(reencrypted).Password);
    }
}
