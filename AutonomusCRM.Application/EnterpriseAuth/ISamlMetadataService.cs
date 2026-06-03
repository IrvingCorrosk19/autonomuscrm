namespace AutonomusCRM.Application.EnterpriseAuth;

public interface ISamlMetadataService
{
    bool IsConfigured { get; }
    string GetServiceProviderMetadataXml(string acsUrl);
    IReadOnlyList<string> GetSoc2TechnicalChecklist();
}
