using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace AutonomusCRM.Infrastructure.DatabaseIntelligence;

internal static partial class DbConnectionStringValidator
{
    private static readonly string[] DangerousTokens =
    [
        ";", "--", "/*", "*/", "xp_", "exec ", "execute ", "drop ", "truncate ",
        "alter ", "create ", "grant ", "revoke ", "shutdown", "password="
    ];

    public static void ValidateConnectionField(string value, string fieldName, int maxLength = 256)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new Application.DatabaseIntelligence.DbIntelligenceValidationException($"{fieldName} is required.");

        var trimmed = value.Trim();
        if (trimmed.Length > maxLength)
            throw new Application.DatabaseIntelligence.DbIntelligenceValidationException($"{fieldName} is too long.");

        foreach (var token in DangerousTokens)
        {
            if (trimmed.Contains(token, StringComparison.OrdinalIgnoreCase))
                throw new Application.DatabaseIntelligence.DbIntelligenceValidationException($"{fieldName} contains invalid characters.");
        }
    }

    public static void ValidateHost(string host)
    {
        ValidateConnectionField(host, "Host", 253);
        if (host.Contains(' ') || host.Contains('\t'))
            throw new Application.DatabaseIntelligence.DbIntelligenceValidationException("Host contains invalid characters.");
    }

    public static void ValidatePort(int port)
    {
        if (port is < 1 or > 65535)
            throw new Application.DatabaseIntelligence.DbIntelligenceValidationException("Port must be between 1 and 65535.");
    }

    public static void ValidateDatabaseName(string databaseName)
        => ValidateConnectionField(databaseName, "Database name", 128);

    public static void ValidateUsername(string username)
        => ValidateConnectionField(username, "Username", 128);

    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
            throw new Application.DatabaseIntelligence.DbIntelligenceValidationException("Password is required.");
        if (password.Length > 512)
            throw new Application.DatabaseIntelligence.DbIntelligenceValidationException("Password is too long.");
    }

    public static void ValidateName(string name)
        => ValidateConnectionField(name, "Connection name", 200);

    public static void ValidateCreateRequest(Application.DatabaseIntelligence.CreateDbConnectionProfileRequest request)
    {
        ValidateName(request.Name);
        ValidateHost(request.Host);
        ValidatePort(request.Port);
        ValidateDatabaseName(request.DatabaseName);
        ValidateUsername(request.Username);
        ValidatePassword(request.Password);
    }

    public static void ValidateTestRequest(Application.DatabaseIntelligence.TestDbConnectionRequest request)
    {
        ValidateHost(request.Host);
        ValidatePort(request.Port);
        ValidateDatabaseName(request.DatabaseName);
        ValidateUsername(request.Username);
        ValidatePassword(request.Password);
    }

    public static string SanitizeErrorMessage(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "Connection failed. Check your credentials and network access.";

        var sanitized = PasswordPattern().Replace(message, "Password=***");
        sanitized = sanitized.Replace('\r', ' ').Replace('\n', ' ');
        if (sanitized.Length > 400)
            sanitized = sanitized[..400];
        return sanitized;
    }

    [GeneratedRegex(@"(?i)(password\s*=\s*)[^\s;]+")]
    private static partial Regex PasswordPattern();
}
