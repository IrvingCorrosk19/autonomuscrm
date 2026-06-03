using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml.Linq;
using AutonomusCRM.Application.EnterpriseAuth;
using Microsoft.Extensions.Options;
using System.Security.Cryptography.Xml;

namespace AutonomusCRM.Infrastructure.EnterpriseAuth;

public sealed class SamlAuthService : ISamlAuthService
{
    private readonly EnterpriseAuthOptions _options;

    public SamlAuthService(IOptions<EnterpriseAuthOptions> options) => _options = options.Value;

    public bool IsAcsConfigured =>
        !string.IsNullOrWhiteSpace(_options.SamlEntityId)
        && !string.IsNullOrWhiteSpace(_options.SamlIdpEntityId);

    public SamlAcsResult ParseAssertion(string samlResponseBase64)
    {
        if (string.IsNullOrWhiteSpace(samlResponseBase64))
            return new(false, null, Array.Empty<string>(), null, "SAMLResponse vacío.");

        try
        {
            var xml = DecodeSamlPayload(samlResponseBase64);
            if (!string.IsNullOrWhiteSpace(_options.SamlCertificate))
            {
                var sigResult = ValidateXmlSignature(xml, _options.SamlCertificate);
                if (!sigResult.Valid)
                    return new(false, null, Array.Empty<string>(), null, sigResult.Error ?? "Firma SAML inválida.");
            }

            var doc = XDocument.Parse(xml, LoadOptions.None);

            var issuer = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "Issuer")?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(_options.SamlIdpEntityId)
                && !string.Equals(issuer, _options.SamlIdpEntityId, StringComparison.OrdinalIgnoreCase))
            {
                return new(false, null, Array.Empty<string>(), null, $"Issuer SAML no coincide: {issuer}");
            }

            var email = ExtractEmail(doc);
            if (string.IsNullOrWhiteSpace(email))
                return new(false, null, Array.Empty<string>(), null, "No se encontró email/NameID en la aserción.");

            var roles = ExtractRoles(doc);
            var tenantId = ParseTenantId();

            return new(true, email, roles, tenantId, null);
        }
        catch (Exception ex)
        {
            return new(false, null, Array.Empty<string>(), null, ex.Message);
        }
    }

    private Guid? ParseTenantId()
    {
        if (Guid.TryParse(_options.SamlDefaultTenantId, out var tid))
            return tid;
        return null;
    }

    private static string DecodeSamlPayload(string encoded)
    {
        var raw = Convert.FromBase64String(encoded.Trim());
        if (raw.Length > 2 && raw[0] == 0x1F && raw[1] == 0x8B)
        {
            using var input = new MemoryStream(raw);
            using var gzip = new GZipStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            gzip.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }

        return Encoding.UTF8.GetString(raw);
    }

    private static string? ExtractEmail(XDocument doc)
    {
        var nameId = doc.Descendants().FirstOrDefault(e => e.Name.LocalName == "NameID")?.Value?.Trim();
        if (!string.IsNullOrWhiteSpace(nameId) && nameId.Contains('@'))
            return nameId;

        foreach (var attr in doc.Descendants().Where(e => e.Name.LocalName == "Attribute"))
        {
            var name = attr.Attribute("Name")?.Value ?? attr.Attribute(XName.Get("Name", "urn:oasis:names:tc:SAML:2.0:assertion"))?.Value;
            if (name is null) continue;
            if (!name.Contains("email", StringComparison.OrdinalIgnoreCase)
                && !name.Contains("mail", StringComparison.OrdinalIgnoreCase)
                && name != "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress")
                continue;

            var value = attr.Descendants().FirstOrDefault(e => e.Name.LocalName == "AttributeValue")?.Value?.Trim();
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        return nameId;
    }

    private static (bool Valid, string? Error) ValidateXmlSignature(string xml, string certificatePemOrBase64)
    {
        try
        {
            var xmlDoc = new System.Xml.XmlDocument { PreserveWhitespace = true };
            xmlDoc.LoadXml(xml);
            var signedXml = new SignedXml(xmlDoc);
            var node = xmlDoc.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl)[0] as System.Xml.XmlElement
                ?? xmlDoc.GetElementsByTagName("Signature")[0] as System.Xml.XmlElement;
            if (node is null)
                return (false, "Assertion SAML sin elemento Signature.");

            signedXml.LoadXml(node);
            var cert = LoadCertificate(certificatePemOrBase64);
            if (cert is null)
                return (false, "Certificado IdP SAML no válido.");
            return signedXml.CheckSignature(cert, verifySignatureOnly: true)
                ? (true, null)
                : (false, "CheckSignature falló para certificado IdP configurado.");
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    private static X509Certificate2? LoadCertificate(string pemOrBase64)
    {
        try
        {
            if (pemOrBase64.Contains("BEGIN CERTIFICATE", StringComparison.Ordinal))
            {
                var lines = pemOrBase64.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(l => !l.StartsWith("-----", StringComparison.Ordinal))
                    .ToArray();
                return new X509Certificate2(Convert.FromBase64String(string.Concat(lines)));
            }

            return new X509Certificate2(Convert.FromBase64String(pemOrBase64.Trim()));
        }
        catch
        {
            return null;
        }
    }

    private static IReadOnlyList<string> ExtractRoles(XDocument doc)
    {
        var roles = new List<string>();
        foreach (var attr in doc.Descendants().Where(e => e.Name.LocalName == "Attribute"))
        {
            var name = attr.Attribute("Name")?.Value ?? "";
            if (!name.Contains("role", StringComparison.OrdinalIgnoreCase)
                && name != "http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                continue;

            foreach (var value in attr.Descendants().Where(e => e.Name.LocalName == "AttributeValue"))
            {
                var r = value.Value?.Trim();
                if (!string.IsNullOrWhiteSpace(r))
                    roles.Add(r);
            }
        }

        return roles;
    }
}
