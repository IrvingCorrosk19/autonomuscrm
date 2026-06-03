# LLM Runtime Audit

## Architecture

```
AddAiRuntime()
  ├── OpenAiLlmProvider
  ├── AzureOpenAiLlmProvider
  ├── AnthropicLlmProvider
  ├── GeminiLlmProvider
  └── ResilientLlmProvider (ILLMProvider + ILlmUsageTracker)
        ├── Provider chain (primary + FallbackProviders)
        ├── Retry (MaxRetries)
        ├── Circuit breaker (CircuitBreakerFailureThreshold/DurationSeconds)
        ├── Rate limit (RequestsPerMinuteLimit)
        └── Cost/token tracking (LlmUsageRecord)
```

## Configuration (`appsettings.json` → `AI`)

| Key | Purpose |
|-----|---------|
| `AI:Provider` | Primary: openai, azure-openai, anthropic, gemini |
| `AI:FallbackProviders[]` | Automatic fallback chain |
| `AI:OpenAI:ApiKey` | OpenAI |
| `AI:AzureOpenAI:Endpoint/ApiKey/Deployment` | Azure |
| `AI:Anthropic:ApiKey` | Anthropic |
| `AI:Gemini:ApiKey` | Google Gemini |

## Health

`ResilientLlmProvider.GetHealth()` → configured providers, open circuits, token totals

## Tests

`AutonomusCRM.Tests/TruthSprint/LlmRuntimeTests.cs` — 11 tests

## Status

| Requirement | Status |
|-------------|--------|
| Provider pattern | ✅ |
| OpenAI | ✅ |
| Azure OpenAI | ✅ |
| Anthropic | ✅ |
| Gemini | ✅ |
| Fallback | ✅ |
| Circuit breaker | ✅ |
| Retry | ✅ |
| Rate limiting | ✅ |
| Token/cost tracking | ✅ |
| Live call without keys | ❌ throws (expected) |
