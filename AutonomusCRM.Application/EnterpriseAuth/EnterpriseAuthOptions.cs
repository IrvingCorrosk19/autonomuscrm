namespace AutonomusCRM.Application.EnterpriseAuth;

public class EnterpriseAuthOptions
{
    public const string SectionName = "EnterpriseAuth";
    public bool Enabled { get; set; }
    public string? OidcAuthority { get; set; }
    public string? OidcClientId { get; set; }
    public string? OidcClientSecret { get; set; }
    public string? SamlEntityId { get; set; }
    public string? SamlMetadataUrl { get; set; }
    public string? SamlCertificate { get; set; }
    public string? SamlIdpEntityId { get; set; }
    /// <summary>Tenant destino para JIT login SAML (Azure AD / Okta / Keycloak).</summary>
    public string? SamlDefaultTenantId { get; set; }
    public string? ScimBearerToken { get; set; }
}
