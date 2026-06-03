using AutonomusCRM.Application.CustomerSuccess;
using AutonomusCRM.Application.Integrations;
using AutonomusCRM.Application.SemanticMemory;
using AutonomusCRM.Infrastructure.Billing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.Infrastructure.Integrations;

public sealed class SecretMaskingService : ISecretMaskingService
{
    public string Mask(string? secret)
    {
        if (string.IsNullOrEmpty(secret)) return "—";
        if (secret.Length <= 8) return "****";
        return secret[..4] + "…" + secret[^4..];
    }

    public string MaskConnection(TenantIntegrationConnection connection)
    {
        var token = connection.AccessToken;
        if (string.IsNullOrEmpty(token)) return "no token";
        if (token.StartsWith("enc:v1:", StringComparison.Ordinal)) return "encrypted-at-rest";
        if (token.StartsWith("plain:", StringComparison.Ordinal)) return Mask(token["plain:".Length..]);
        return Mask(token);
    }
}

public sealed class IntegrationWebhookAuditor : IIntegrationWebhookAuditor
{
    private readonly ILogger<IntegrationWebhookAuditor> _logger;

    public IntegrationWebhookAuditor(ILogger<IntegrationWebhookAuditor> logger) => _logger = logger;

    public void LogReceived(string provider, string eventType, Guid? tenantId, bool signatureValid, string? detail = null)
    {
        _logger.LogInformation(
            "Webhook audit: Provider={Provider} Event={EventType} TenantId={TenantId} SignatureValid={Valid} Detail={Detail}",
            provider, eventType, tenantId, signatureValid, detail ?? "");
    }
}

public sealed class IntegrationHealthService : IIntegrationHealthService
{
    private readonly ITenantIntegrationRepository _repo;
    private readonly IIntegrationOAuthService _oauth;
    private readonly IProductionEmbeddingProvider _embeddings;
    private readonly IIntegrationTokenProtector _protector;
    private readonly CommunicationOptions _comms;
    private readonly StripeBillingOptions _stripe;
    private readonly TwilioOptions _twilio;

    public IntegrationHealthService(
        ITenantIntegrationRepository repo,
        IIntegrationOAuthService oauth,
        IProductionEmbeddingProvider embeddings,
        IIntegrationTokenProtector protector,
        IOptions<CommunicationOptions> comms,
        IOptions<StripeBillingOptions> stripe,
        IOptions<TwilioOptions> twilio)
    {
        _repo = repo;
        _oauth = oauth;
        _embeddings = embeddings;
        _protector = protector;
        _comms = comms.Value;
        _stripe = stripe.Value;
        _twilio = twilio.Value;
    }

    public async Task<IntegrationHealthDashboardDto> GetDashboardAsync(Guid tenantId, CancellationToken cancellationToken = default)
    {
        var connections = await _repo.ListAsync(tenantId, cancellationToken);
        var items = IntegrationProviderCatalog.All.Select(p => BuildItem(p, connections)).ToList();
        return new IntegrationHealthDashboardDto(
            tenantId,
            items,
            _protector.EncryptionConfigured,
            _protector.EncryptionConfigured ? "AES-GCM at rest" : "plain: prefix (set IntegrationEncryption:Key)");
    }

    private IntegrationHealthItemDto BuildItem(string provider, IReadOnlyList<TenantIntegrationConnection> connections)
    {
        var conn = connections.FirstOrDefault(c => c.Provider == provider);
        var tenantOverride = conn?.IsEnabled == true;
        return provider switch
        {
            IntegrationProviders.OpenAI => BuildGlobalAi("openai", provider, conn, tenantOverride),
            IntegrationProviders.AzureOpenAI => BuildGlobalAi("azure-openai", provider, conn, tenantOverride),
            IntegrationProviders.SendGrid => BuildComms(provider, conn, tenantOverride,
                !string.IsNullOrWhiteSpace(conn?.AccessToken) || !string.IsNullOrWhiteSpace(_comms.SendGridApiKey),
                ["Communications:SendGridApiKey", "SENDGRID_API_KEY"]),
            IntegrationProviders.Smtp => BuildComms(provider, conn, tenantOverride,
                !string.IsNullOrWhiteSpace(_comms.SmtpHost) && !string.IsNullOrWhiteSpace(_comms.SmtpUser),
                ["Communications:SmtpHost", "Communications:SmtpUser", "Communications:SmtpPassword"]),
            IntegrationProviders.Twilio => BuildComms(provider, conn, tenantOverride,
                !string.IsNullOrWhiteSpace(conn?.AccessToken) ||
                (!string.IsNullOrWhiteSpace(_twilio.AccountSid) && !string.IsNullOrWhiteSpace(_twilio.AuthToken)),
                ["Twilio:AccountSid", "Twilio:AuthToken"]),
            IntegrationProviders.WhatsApp => BuildComms(provider, conn, tenantOverride,
                !string.IsNullOrWhiteSpace(conn?.AccessToken) ||
                (!string.IsNullOrWhiteSpace(_comms.WhatsAppAccessToken) && !string.IsNullOrWhiteSpace(_comms.WhatsAppPhoneNumberId)),
                ["Communications:WhatsAppAccessToken", "Communications:WhatsAppPhoneNumberId"]),
            IntegrationProviders.Stripe => BuildTenantCrm(provider, conn, tenantOverride,
                !string.IsNullOrWhiteSpace(conn?.AccessToken) || !string.IsNullOrWhiteSpace(_stripe.SecretKey),
                false,
                ["Stripe:SecretKey", "Stripe:WebhookSecret"]),
            IntegrationProviders.HubSpot or IntegrationProviders.Salesforce
                or IntegrationProviders.Gmail or IntegrationProviders.Outlook =>
                BuildTenantCrm(provider, conn, tenantOverride,
                    !string.IsNullOrWhiteSpace(conn?.AccessToken),
                    _oauth.IsOAuthConfigured(provider),
                    ProviderRequiredVars(provider)),
            _ => new IntegrationHealthItemDto(provider, IntegrationHealthStates.Misconfigured, false, false, false,
                "unknown", null, null, "Unknown provider", Array.Empty<string>())
        };
    }

    private IntegrationHealthItemDto BuildGlobalAi(string key, string provider, TenantIntegrationConnection? conn, bool tenantOverride)
    {
        var status = _embeddings.GetStatus();
        var active = status.ActiveProvider == key || (key == "openai" && status.OpenAiConfigured) || (key == "azure-openai" && status.AzureOpenAiConfigured);
        var tenantToken = !string.IsNullOrWhiteSpace(conn?.AccessToken);
        var configured = active || tenantToken;
        var state = configured
            ? (status.IsProductionProvider || tenantToken ? IntegrationHealthStates.Connected : IntegrationHealthStates.Pending)
            : IntegrationHealthStates.Disconnected;
        return new IntegrationHealthItemDto(
            provider, state, configured, tenantOverride && tenantToken, configured,
            tenantToken ? "tenant" : "global",
            conn?.LastSyncStatus, conn?.LastSyncAt,
            status.Badge,
            key == "azure-openai"
                ? ["AI:AzureOpenAI:Endpoint", "AI:AzureOpenAI:ApiKey", "AI:AzureOpenAI:EmbeddingDeployment"]
                : ["AI:ApiKey", "AI:OpenAI:ApiKey"]);
    }

    private IntegrationHealthItemDto BuildComms(string provider, TenantIntegrationConnection? conn, bool tenantOverride, bool creds, string[] vars)
    {
        var state = creds
            ? MapSync(conn?.LastSyncStatus, IntegrationHealthStates.Connected)
            : IntegrationHealthStates.Disconnected;
        if (creds && conn?.LastSyncStatus?.StartsWith("Error", StringComparison.OrdinalIgnoreCase) == true)
            state = IntegrationHealthStates.Error;
        return new IntegrationHealthItemDto(
            provider, state, creds, tenantOverride, creds,
            tenantOverride ? "tenant" : "global",
            conn?.LastSyncStatus, conn?.LastSyncAt,
            creds ? "Credentials present" : "Missing credentials — smoke BLOCKED",
            vars);
    }

    private IntegrationHealthItemDto BuildTenantCrm(string provider, TenantIntegrationConnection? conn, bool tenantOverride, bool creds, bool oauthReady, string[] vars)
    {
        if (!oauthReady && !creds)
            return new IntegrationHealthItemDto(provider, IntegrationHealthStates.Misconfigured, false, false, false,
                "global", conn?.LastSyncStatus, conn?.LastSyncAt,
                "OAuth app credentials missing in IntegrationOAuth section", vars);

        if (!creds)
            return new IntegrationHealthItemDto(provider, IntegrationHealthStates.Disconnected, oauthReady, false, false,
                "global", null, null, oauthReady ? "OAuth ready — tenant not connected" : "Not configured", vars);

        var state = MapSync(conn!.LastSyncStatus, IntegrationHealthStates.Connected);
        if (conn.LastSyncStatus?.Contains("expired", StringComparison.OrdinalIgnoreCase) == true)
            state = IntegrationHealthStates.Expired;
        return new IntegrationHealthItemDto(provider, state, true, tenantOverride, true,
            "tenant", conn.LastSyncStatus, conn.LastSyncAt, "Tenant connection active", vars);
    }

    private static string MapSync(string? sync, string whenOk)
    {
        if (string.IsNullOrEmpty(sync)) return IntegrationHealthStates.Pending;
        if (sync is "error" or "failed" || sync.StartsWith("Error", StringComparison.OrdinalIgnoreCase))
            return IntegrationHealthStates.Error;
        if (sync is "Configured" or "OK" or "ok" or "success") return whenOk;
        return whenOk;
    }

    private static string[] ProviderRequiredVars(string provider) => provider switch
    {
        IntegrationProviders.HubSpot => ["IntegrationOAuth:HubSpotClientId", "IntegrationOAuth:HubSpotClientSecret"],
        IntegrationProviders.Salesforce => ["IntegrationOAuth:SalesforceClientId", "IntegrationOAuth:SalesforceClientSecret"],
        IntegrationProviders.Gmail => ["IntegrationOAuth:GoogleClientId", "IntegrationOAuth:GoogleClientSecret"],
        IntegrationProviders.Outlook => ["IntegrationOAuth:MicrosoftClientId", "IntegrationOAuth:MicrosoftClientSecret"],
        IntegrationProviders.Stripe => ["Stripe:SecretKey", "Stripe:WebhookSecret"],
        _ => Array.Empty<string>()
    };
}

public sealed class IntegrationSmokeTestService : IIntegrationSmokeTestService
{
    private readonly IIntegrationHealthService _health;

    public IntegrationSmokeTestService(IIntegrationHealthService health) => _health = health;

    public IReadOnlyList<string> GetSupportedProviders() => IntegrationProviderCatalog.All.ToList();

    public async Task<SmokeTestResultDto> RunAsync(string provider, Guid? tenantId = null, CancellationToken cancellationToken = default)
    {
        if (tenantId is null || tenantId == Guid.Empty)
            return Blocked(provider, "TenantId required for smoke test context");

        var dash = await _health.GetDashboardAsync(tenantId.Value, cancellationToken);
        var item = dash.Providers.FirstOrDefault(p => p.Provider == provider);
        if (item is null)
            return Blocked(provider, "Unknown provider");

        if (!item.CredentialsPresent)
            return new SmokeTestResultDto(provider, IntegrationHealthStates.Blocked,
                "BLOCKED — credentials not configured. Set required variables and reconnect.",
                true, item.RequiredVariables);

        if (item.Status is IntegrationHealthStates.Error or IntegrationHealthStates.Misconfigured)
            return new SmokeTestResultDto(provider, IntegrationHealthStates.Blocked,
                $"BLOCKED — health status {item.Status}: {item.Detail}",
                false, item.RequiredVariables);

        return new SmokeTestResultDto(provider, IntegrationHealthStates.Pending,
            "READY — credentials detected. Live HTTP smoke requires explicit opt-in with INTEGRATION_SMOKE_LIVE=1.",
            false, item.RequiredVariables);
    }

    private static SmokeTestResultDto Blocked(string provider, string msg) =>
        new(provider, IntegrationHealthStates.Blocked, msg, true, Array.Empty<string>());
}

public static class IntegrationResilience
{
    private static readonly Dictionary<string, (int Failures, DateTime OpenUntil)> Breakers = new();
    private const int Threshold = 5;
    private static readonly TimeSpan OpenDuration = TimeSpan.FromMinutes(2);

    public static bool IsCircuitOpen(string provider)
    {
        lock (Breakers)
        {
            if (!Breakers.TryGetValue(provider, out var s)) return false;
            if (DateTime.UtcNow < s.OpenUntil) return true;
            Breakers.Remove(provider);
            return false;
        }
    }

    public static void RecordFailure(string provider)
    {
        lock (Breakers)
        {
            var failures = Breakers.TryGetValue(provider, out var s) ? s.Failures + 1 : 1;
            if (failures >= Threshold)
                Breakers[provider] = (failures, DateTime.UtcNow.Add(OpenDuration));
            else
                Breakers[provider] = (failures, DateTime.MinValue);
        }
    }

    public static void RecordSuccess(string provider)
    {
        lock (Breakers) { Breakers.Remove(provider); }
    }

    public static async Task<HttpResponseMessage> SendWithRetryAsync(
        Func<Task<HttpResponseMessage>> send, string provider, int maxAttempts = 3, CancellationToken cancellationToken = default)
    {
        if (IsCircuitOpen(provider))
            throw new InvalidOperationException($"Circuit open for {provider}");

        Exception? last = null;
        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                var response = await send();
                if ((int)response.StatusCode == 429)
                {
                    RecordFailure(provider);
                    if (attempt < maxAttempts)
                    {
                        await Task.Delay(TimeSpan.FromMilliseconds(200 * attempt), cancellationToken);
                        continue;
                    }
                }
                if (response.IsSuccessStatusCode)
                    RecordSuccess(provider);
                else if ((int)response.StatusCode >= 500)
                    RecordFailure(provider);
                return response;
            }
            catch (Exception ex)
            {
                last = ex;
                RecordFailure(provider);
                if (attempt < maxAttempts)
                    await Task.Delay(TimeSpan.FromMilliseconds(150 * attempt), cancellationToken);
            }
        }

        throw last ?? new InvalidOperationException($"Request failed for {provider}");
    }
}
