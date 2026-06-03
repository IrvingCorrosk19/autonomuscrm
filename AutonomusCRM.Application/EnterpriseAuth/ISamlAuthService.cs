namespace AutonomusCRM.Application.EnterpriseAuth;

public record SamlAcsResult(
    bool Success,
    string? Email,
    IReadOnlyList<string> Roles,
    Guid? TenantId,
    string? Error);

public interface ISamlAuthService
{
    bool IsAcsConfigured { get; }
    SamlAcsResult ParseAssertion(string samlResponseBase64);
}
