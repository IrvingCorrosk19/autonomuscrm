using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Application.Integrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.SemanticMemory;

public sealed class ProductionEmbeddingProvider : IProductionEmbeddingProvider
{
    private readonly IHttpClientFactory _httpFactory;
    private readonly IConfiguration _config;
    private readonly IntegrationEndpointsOptions _endpoints;
    private readonly ILogger<ProductionEmbeddingProvider> _logger;

    public ProductionEmbeddingProvider(
        IHttpClientFactory httpFactory,
        IConfiguration config,
        IOptions<IntegrationEndpointsOptions> endpoints,
        ILogger<ProductionEmbeddingProvider> logger)
    {
        _httpFactory = httpFactory;
        _config = config;
        _endpoints = endpoints.Value;
        _logger = logger;
    }

    public ProductionEmbeddingStatus GetStatus()
    {
        var openAiKey = _config["AI:ApiKey"] ?? _config["AI:OpenAI:ApiKey"];
        var azureKey = _config["AI:AzureOpenAI:ApiKey"];
        var azureEndpoint = _config["AI:AzureOpenAI:Endpoint"];
        var provider = _config["AI:EmbeddingProvider"] ?? _config["AI:Provider"] ?? "local-fallback";

        if (!string.IsNullOrWhiteSpace(azureKey) && !string.IsNullOrWhiteSpace(azureEndpoint))
            return new ProductionEmbeddingStatus("azure-openai", true, "Azure OpenAI Embeddings", !string.IsNullOrWhiteSpace(openAiKey), true);

        if (!string.IsNullOrWhiteSpace(openAiKey) && provider.Contains("openai", StringComparison.OrdinalIgnoreCase))
            return new ProductionEmbeddingStatus("openai", true, "OpenAI Embeddings", true, false);

        return new ProductionEmbeddingStatus("local-deterministic", false, "Local deterministic fallback (not production embeddings)", false, false);
    }

    public async Task<ProductionEmbeddingResult> EmbedAsync(string text, CancellationToken cancellationToken = default)
    {
        var status = GetStatus();
        if (status.ActiveProvider == "azure-openai")
            return await EmbedAzureAsync(text, status, cancellationToken);
        if (status.ActiveProvider == "openai")
            return await EmbedOpenAiAsync(text, status, cancellationToken);

        return LocalFallback(text, status);
    }

    private async Task<ProductionEmbeddingResult> EmbedOpenAiAsync(string text, ProductionEmbeddingStatus status, CancellationToken cancellationToken)
    {
        var apiKey = _config["AI:ApiKey"] ?? _config["AI:OpenAI:ApiKey"]!;
        var model = _config["AI:EmbeddingModel"] ?? "text-embedding-3-small";
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        var payload = JsonSerializer.Serialize(new { input = text, model });
        using var response = await client.PostAsync(
            _endpoints.OpenAiEmbeddingsUrl,
            new StringContent(payload, Encoding.UTF8, "application/json"),
            cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("OpenAI embedding failed {Status}, using fallback", response.StatusCode);
            return LocalFallback(text, status with { Badge = "OpenAI failed — local fallback" });
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var vector = doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray()
            .Select(e => (float)e.GetDouble()).ToArray();
        return new ProductionEmbeddingResult(vector, "openai", model, true, status.Badge);
    }

    private async Task<ProductionEmbeddingResult> EmbedAzureAsync(string text, ProductionEmbeddingStatus status, CancellationToken cancellationToken)
    {
        var apiKey = _config["AI:AzureOpenAI:ApiKey"]!;
        var endpoint = _config["AI:AzureOpenAI:Endpoint"]!.TrimEnd('/');
        var deployment = _config["AI:AzureOpenAI:EmbeddingDeployment"] ?? "text-embedding-ada-002";
        var client = _httpFactory.CreateClient();
        client.DefaultRequestHeaders.Add("api-key", apiKey);
        var payload = JsonSerializer.Serialize(new { input = text });
        var url = $"{endpoint}/openai/deployments/{deployment}/embeddings?api-version=2024-02-01";
        using var response = await client.PostAsync(url, new StringContent(payload, Encoding.UTF8, "application/json"), cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Azure embedding failed {Status}, using fallback", response.StatusCode);
            return LocalFallback(text, status with { Badge = "Azure failed — local fallback" });
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var vector = doc.RootElement.GetProperty("data")[0].GetProperty("embedding").EnumerateArray()
            .Select(e => (float)e.GetDouble()).ToArray();
        return new ProductionEmbeddingResult(vector, "azure-openai", deployment, true, status.Badge);
    }

    private static ProductionEmbeddingResult LocalFallback(string text, ProductionEmbeddingStatus status)
    {
        var hash = text.GetHashCode();
        var vector = new float[32];
        for (var i = 0; i < vector.Length; i++)
            vector[i] = ((hash >> (i % 16)) & 0xFF) / 255f;
        return new ProductionEmbeddingResult(vector, status.ActiveProvider, "local-deterministic-v1", false, status.Badge);
    }
}
