using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.CustomerSuccess;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.CustomerSuccess;

public sealed class WhatsAppBusinessDeliveryProvider : IWhatsAppDeliveryProvider
{
    private readonly CommunicationOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<WhatsAppBusinessDeliveryProvider> _logger;

    public WhatsAppBusinessDeliveryProvider(
        IOptions<CommunicationOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<WhatsAppBusinessDeliveryProvider> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task<(bool Success, string? Error)> SendAsync(
        string phone, string message, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.WhatsAppAccessToken) ||
            string.IsNullOrWhiteSpace(_options.WhatsAppPhoneNumberId))
            return (false, "WhatsApp Business API not configured");

        var version = _options.WhatsAppApiVersion ?? "v21.0";
        var url = $"https://graph.facebook.com/{version}/{_options.WhatsAppPhoneNumberId}/messages";
        var payload = new
        {
            messaging_product = "whatsapp",
            to = phone.TrimStart('+'),
            type = "text",
            text = new { body = message }
        };

        var client = _httpClientFactory.CreateClient("WhatsApp");
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.WhatsAppAccessToken);
        request.Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

        var response = await client.SendAsync(request, cancellationToken);
        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("WhatsApp message sent to {Phone}", phone);
            return (true, null);
        }

        var err = await response.Content.ReadAsStringAsync(cancellationToken);
        _logger.LogWarning("WhatsApp send failed: {Error}", err);
        return (false, err);
    }
}
