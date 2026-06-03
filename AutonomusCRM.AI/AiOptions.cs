namespace AutonomusCRM.AI;

public class AiOptions
{
    public const string SectionName = "AI";

    public bool Enabled { get; set; } = true;
    public string Provider { get; set; } = "openai";
    public string[] FallbackProviders { get; set; } = ["azure-openai", "anthropic", "gemini"];
    public string Model { get; set; } = "gpt-4o-mini";
    public int MaxRetries { get; set; } = 3;
    public int CircuitBreakerFailureThreshold { get; set; } = 5;
    public int CircuitBreakerDurationSeconds { get; set; } = 60;
    public int MaxTokensPerRequest { get; set; } = 4096;
    public double RequestsPerMinuteLimit { get; set; } = 60;

    public OpenAiProviderOptions OpenAI { get; set; } = new();
    public AzureOpenAiProviderOptions AzureOpenAI { get; set; } = new();
    public AnthropicProviderOptions Anthropic { get; set; } = new();
    public GeminiProviderOptions Gemini { get; set; } = new();

    /// <summary>Legacy flat keys — mapped in DI when nested empty.</summary>
    public string ApiKey { get; set; } = "";
    public string Endpoint { get; set; } = "";
}

public sealed class OpenAiProviderOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

public sealed class AzureOpenAiProviderOptions
{
    public string Endpoint { get; set; } = "";
    public string ApiKey { get; set; } = "";
    public string Deployment { get; set; } = "gpt-4o-mini";
    public string ApiVersion { get; set; } = "2024-02-15-preview";
}

public sealed class AnthropicProviderOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "claude-3-5-haiku-20241022";
    public string BaseUrl { get; set; } = "https://api.anthropic.com/v1";
}

public sealed class GeminiProviderOptions
{
    public string ApiKey { get; set; } = "";
    public string Model { get; set; } = "gemini-2.0-flash";
    public string BaseUrl { get; set; } = "https://generativelanguage.googleapis.com/v1beta";
}
