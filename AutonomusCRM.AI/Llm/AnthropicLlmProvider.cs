using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI.Llm;

public sealed class AnthropicLlmProvider : ILlmProviderImplementation
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AiOptions _options;
    private readonly ILogger<AnthropicLlmProvider> _logger;

    public AnthropicLlmProvider(IHttpClientFactory httpFactory, IOptions<AiOptions> options, ILogger<AnthropicLlmProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => "anthropic";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Anthropic.ApiKey);

    public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new LlmNotConfiguredException("Anthropic ApiKey not configured (AI:Anthropic:ApiKey).");

        var model = request.Model ?? _options.Anthropic.Model;
        var maxTokens = request.MaxTokens ?? _options.MaxTokensPerRequest;
        var baseUrl = _options.Anthropic.BaseUrl.TrimEnd('/');

        var payload = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            system = request.SystemPrompt,
            messages = new[] { new { role = "user", content = request.UserPrompt } }
        });

        var client = _httpFactory.CreateClient("LlmAnthropic");
        client.DefaultRequestHeaders.Add("x-api-key", _options.Anthropic.ApiKey);
        client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
        using var response = await client.PostAsync(
            $"{baseUrl}/messages",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Anthropic failed {Status}", response.StatusCode);
            throw new LlmProviderUnavailableException(ProviderId, $"{response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
        var tokens = doc.RootElement.TryGetProperty("usage", out var usage)
            ? usage.GetProperty("input_tokens").GetInt32() + usage.GetProperty("output_tokens").GetInt32()
            : 0;
        return new LlmCompletionResult(content, tokens, IsPlaceholder: false, ProviderId, model);
    }
}
