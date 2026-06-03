namespace AutonomusCRM.Application.Integrations;

public interface IIntegrationTokenProtector
{
    string Protect(string? plaintext);
    string? Unprotect(string? stored);
    bool EncryptionConfigured { get; }
}
