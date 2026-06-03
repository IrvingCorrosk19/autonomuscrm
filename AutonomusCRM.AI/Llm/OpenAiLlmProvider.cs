using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI.Llm;

public sealed class OpenAiLlmProvider : ILlmProviderImplementation
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AiOptions _options;
    private readonly ILogger<OpenAiLlmProvider> _logger;

    public OpenAiLlmProvider(IHttpClientFactory httpFactory, IOptions<AiOptions> options, ILogger<OpenAiLlmProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => "openai";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey) || !string.IsNullOrWhiteSpace(_options.ApiKey);

    public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken)
    {
        var apiKey = !string.IsNullOrWhiteSpace(_options.OpenAI.ApiKey) ? _options.OpenAI.ApiKey : _options.ApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new LlmNotConfiguredException("OpenAI ApiKey not configured (AI:OpenAI:ApiKey or AI:ApiKey).");

        var model = request.Model ?? _options.OpenAI.Model ?? _options.Model ?? "gpt-4o-mini";
        var maxTokens = request.MaxTokens ?? _options.MaxTokensPerRequest;
        var baseUrl = string.IsNullOrWhiteSpace(_options.OpenAI.BaseUrl) ? "https://api.openai.com/v1" : _options.OpenAI.BaseUrl.TrimEnd('/');

        var payload = JsonSerializer.Serialize(new
        {
            model,
            max_tokens = maxTokens,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        });

        var client = _httpFactory.CreateClient("LlmOpenAI");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        using var response = await client.PostAsync(
            $"{baseUrl}/chat/completions",
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI completion failed {Status}: {Body}", response.StatusCode, body[..Math.Min(200, body.Length)]);
            throw new LlmProviderUnavailableException(ProviderId, $"{response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var tokens = doc.RootElement.TryGetProperty("usage", out var usage)
            ? usage.GetProperty("total_tokens").GetInt32()
            : 0;
        return new LlmCompletionResult(content, tokens, IsPlaceholder: false, ProviderId, model);
    }
}
