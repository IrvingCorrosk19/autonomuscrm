using AutonomusCRM.Application.EnterpriseAuth;
using AutonomusCRM.Infrastructure.EnterpriseAuth;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Tests.EnterpriseAuth;

public class SamlMetadataTests
{
    [Fact]
    public void Metadata_ContainsEntityId_WhenConfigured()
    {
        var svc = new SamlMetadataService(Options.Create(new EnterpriseAuthOptions
        {
            SamlEntityId = "urn:af:sp",
            SamlIdpEntityId = "urn:okta:idp"
        }));
        Assert.True(svc.IsConfigured);
        var xml = svc.GetServiceProviderMetadataXml("https://app.example.com/acs");
        Assert.Contains("urn:af:sp", xml);
    }

    [Fact]
    public void Soc2Checklist_HasMinimumControls()
    {
        var svc = new SamlMetadataService(Options.Create(new EnterpriseAuthOptions()));
        Assert.True(svc.GetSoc2TechnicalChecklist().Count >= 8);
    }
}
