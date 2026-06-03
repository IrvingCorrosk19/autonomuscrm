using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI.Llm;

public sealed class AzureOpenAiLlmProvider : ILlmProviderImplementation
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AiOptions _options;
    private readonly ILogger<AzureOpenAiLlmProvider> _logger;

    public AzureOpenAiLlmProvider(IHttpClientFactory httpFactory, IOptions<AiOptions> options, ILogger<AzureOpenAiLlmProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => "azure-openai";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(_options.AzureOpenAI.ApiKey) &&
        !string.IsNullOrWhiteSpace(_options.AzureOpenAI.Endpoint);

    public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken)
    {
        var cfg = _options.AzureOpenAI;
        if (!IsConfigured)
            throw new LlmNotConfiguredException("Azure OpenAI not configured (AI:AzureOpenAI:Endpoint + ApiKey).");

        var deployment = request.Model ?? cfg.Deployment;
        var maxTokens = request.MaxTokens ?? _options.MaxTokensPerRequest;
        var endpoint = cfg.Endpoint.TrimEnd('/');
        var url = $"{endpoint}/openai/deployments/{deployment}/chat/completions?api-version={cfg.ApiVersion}";

        var payload = JsonSerializer.Serialize(new
        {
            max_tokens = maxTokens,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt },
                new { role = "user", content = request.UserPrompt }
            }
        });

        var client = _httpFactory.CreateClient("LlmAzure");
        client.DefaultRequestHeaders.Add("api-key", cfg.ApiKey);
        using var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"), cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure OpenAI failed {Status}", response.StatusCode);
            throw new LlmProviderUnavailableException(ProviderId, $"{response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
        var tokens = doc.RootElement.TryGetProperty("usage", out var usage)
            ? usage.GetProperty("total_tokens").GetInt32()
            : 0;
        return new LlmCompletionResult(content, tokens, IsPlaceholder: false, ProviderId, deployment);
    }
}
