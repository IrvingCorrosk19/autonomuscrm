using System.Text;
using AutonomusCRM.Application.EnterpriseAuth;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.EnterpriseAuth;

public sealed class SamlMetadataService : ISamlMetadataService
{
    private readonly EnterpriseAuthOptions _options;

    public SamlMetadataService(IOptions<EnterpriseAuthOptions> options) => _options = options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.SamlEntityId)
        && !string.IsNullOrWhiteSpace(_options.SamlIdpEntityId);

    public string GetServiceProviderMetadataXml(string acsUrl)
    {
        var entityId = _options.SamlEntityId ?? "urn:autonomusflow:sp";
        return $"""
            <?xml version="1.0"?>
            <EntityDescriptor xmlns="urn:oasis:names:tc:SAML:2.0:metadata" entityID="{entityId}">
              <SPSSODescriptor AuthnRequestsSigned="false" WantAssertionsSigned="true" protocolSupportEnumeration="urn:oasis:names:tc:SAML:2.0:protocol">
                <AssertionConsumerService Binding="urn:oasis:names:tc:SAML:2.0:bindings:HTTP-POST" Location="{acsUrl}" index="0"/>
              </SPSSODescriptor>
            </EntityDescriptor>
            """;
    }

    public IReadOnlyList<string> GetSoc2TechnicalChecklist() =>
    [
        "Multi-tenant isolation (EF global filters)",
        "HITL Trust Inbox with audit trail",
        "JWT + refresh token rotation",
        "SCIM users + groups provisioning",
        "Encryption in transit (TLS) — configure reverse proxy",
        "Secrets via environment — no keys in repo",
        "Structured logging + OTel traces",
        "Automated DB migrations with rollback scripts",
        "AI kill switch (AI__Enabled)",
        "Failed event DLQ (FailedEventMessages)"
    ];
}
