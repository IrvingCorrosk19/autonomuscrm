using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.AI.Llm;

public sealed class GeminiLlmProvider : ILlmProviderImplementation
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly AiOptions _options;
    private readonly ILogger<GeminiLlmProvider> _logger;

    public GeminiLlmProvider(IHttpClientFactory httpFactory, IOptions<AiOptions> options, ILogger<GeminiLlmProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public string ProviderId => "gemini";

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Gemini.ApiKey);

    public async Task<LlmCompletionResult> CompleteAsync(LlmCompletionRequest request, CancellationToken cancellationToken)
    {
        if (!IsConfigured)
            throw new LlmNotConfiguredException("Gemini ApiKey not configured (AI:Gemini:ApiKey).");

        var model = request.Model ?? _options.Gemini.Model;
        var baseUrl = _options.Gemini.BaseUrl.TrimEnd('/');
        var url = $"{baseUrl}/models/{model}:generateContent?key={Uri.EscapeDataString(_options.Gemini.ApiKey)}";

        var payload = JsonSerializer.Serialize(new
        {
            systemInstruction = new { parts = new[] { new { text = request.SystemPrompt } } },
            contents = new[] { new { role = "user", parts = new[] { new { text = request.UserPrompt } } } }
        });

        var client = _httpFactory.CreateClient("LlmGemini");
        using var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"), cancellationToken);

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Gemini failed {Status}", response.StatusCode);
            throw new LlmProviderUnavailableException(ProviderId, $"{response.StatusCode}");
        }

        using var doc = JsonDocument.Parse(body);
        var content = doc.RootElement.GetProperty("candidates")[0].GetProperty("content").GetProperty("parts")[0].GetProperty("text").GetString() ?? "";
        return new LlmCompletionResult(content, EstimateTokens(request), IsPlaceholder: false, ProviderId, model);
    }

    private static int EstimateTokens(LlmCompletionRequest request) =>
        (request.SystemPrompt.Length + request.UserPrompt.Length) / 4;
}
