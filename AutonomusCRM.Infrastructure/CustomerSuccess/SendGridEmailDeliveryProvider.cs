using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public sealed class SendGridEmailDeliveryProvider : IEmailDeliveryProvider
{
    private readonly CommunicationOptions _options;
    private readonly IntegrationEndpointsOptions _endpoints;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SendGridEmailDeliveryProvider> _logger;

    public SendGridEmailDeliveryProvider(
        IOptions<CommunicationOptions> options,
        IOptions<IntegrationEndpointsOptions> endpoints,
        IHttpClientFactory httpClientFactory,
        ILogger<SendGridEmailDeliveryProvider> logger)
    {
        _options = options.Value;
        _endpoints = endpoints.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> SendAsync(
        string to, string subject, string body, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.SendGridApiKey))
            return (false, "SendGrid API key not configured");

        var payload = new
        {
            personalizations = new[] { new { to = new[] { new { email = to } } } },
            from = new { email = _options.FromAddress, name = _options.FromName },
            subject,
            content = new[] { new { type = "text/html", value = body } }
        };

        var client = _httpClientFactory.CreateClient("SendGrid");
        using var request = new HttpRequestMessage(HttpMethod.Post, _endpoints.SendGridMailUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.SendGridApiKey);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("SendGrid email sent to {To}", to);
            return (true, null);
        }

        var err = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("SendGrid failed {Status}: {Error}", response.StatusCode, err);
        return (false, err);
    }
}
