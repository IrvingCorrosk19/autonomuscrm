using System.Text;
using AutonomusCRM.Application.EnterpriseAuth;
using AutonomusCRM.Infrastructure.EnterpriseAuth;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Tests.EnterpriseAuth;

public class SamlAuthServiceTests
{
    private const string IdpEntityId = "urn:test:idp";

    [Fact]
    public void ParseAssertion_ExtractsEmail_WhenIssuerMatches()
    {
        var xml = $"""
            <?xml version="1.0"?>
            <samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol">
              <saml:Assertion xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion">
                <saml:Issuer>{IdpEntityId}</saml:Issuer>
                <saml:Subject>
                  <saml:NameID>sso.user@enterprise.com</saml:NameID>
                </saml:Subject>
                <saml:AttributeStatement>
                  <saml:Attribute Name="http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress">
                    <saml:AttributeValue>sso.user@enterprise.com</saml:AttributeValue>
                  </saml:Attribute>
                  <saml:Attribute Name="http://schemas.microsoft.com/ws/2008/06/identity/claims/role">
                    <saml:AttributeValue>Admin</saml:AttributeValue>
                  </saml:Attribute>
                </saml:AttributeStatement>
              </saml:Assertion>
            </samlp:Response>
            """;

        var svc = CreateService();
        var encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml));
        var result = svc.ParseAssertion(encoded);

        Assert.True(result.Success);
        Assert.Equal("sso.user@enterprise.com", result.Email);
        Assert.Contains("Admin", result.Roles);
    }

    [Fact]
    public void ParseAssertion_Rejects_WrongIssuer()
    {
        var xml = """
            <?xml version="1.0"?>
            <samlp:Response xmlns:samlp="urn:oasis:names:tc:SAML:2.0:protocol">
              <saml:Assertion xmlns:saml="urn:oasis:names:tc:SAML:2.0:assertion">
                <saml:Issuer>urn:evil:idp</saml:Issuer>
                <saml:Subject><saml:NameID>a@b.com</saml:NameID></saml:Subject>
              </saml:Assertion>
            </samlp:Response>
            """;
        var svc = CreateService();
        var result = svc.ParseAssertion(Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)));
        Assert.False(result.Success);
        Assert.Contains("Issuer", result.Error ?? "");
    }

    [Fact]
    public void IsAcsConfigured_RequiresEntityIds()
    {
        var svc = new SamlAuthService(Options.Create(new EnterpriseAuthOptions
        {
            SamlEntityId = "urn:sp",
            SamlIdpEntityId = IdpEntityId
        }));
        Assert.True(svc.IsAcsConfigured);
    }

    private static SamlAuthService CreateService() =>
        new(Options.Create(new EnterpriseAuthOptions
        {
            SamlEntityId = "urn:autonomus:sp",
            SamlIdpEntityId = IdpEntityId,
            SamlDefaultTenantId = Guid.NewGuid().ToString()
        }));
}
