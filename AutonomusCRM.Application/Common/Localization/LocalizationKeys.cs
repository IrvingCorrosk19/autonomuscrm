namespace AutonomusCRM.Application.Common.Localization;

/// <summary>Resource keys for user-facing errors (resolved in API/UI via IStringLocalizer).</summary>
public static class LocalizationKeys
{
    public const string Auth_InvalidCredentials = "Auth_InvalidCredentials";
    public const string Auth_InvalidMfaToken = "Auth_InvalidMfaToken";
    public const string Auth_InvalidMfaCode = "Auth_InvalidMfaCode";
    public const string Auth_InvalidToken = "Auth_InvalidToken";
    public const string Auth_MfaNotEnabled = "Auth_MfaNotEnabled";
    public const string Auth_InvalidRefreshToken = "Auth_InvalidRefreshToken";
    public const string Auth_InvalidUser = "Auth_InvalidUser";
    public const string Auth_TenantUnauthorized = "Auth_TenantUnauthorized";

    public const string Error_NotFound_Customer = "Error_NotFound_Customer";
    public const string Error_NotFound_Deal = "Error_NotFound_Deal";
    public const string Error_NotFound_Lead = "Error_NotFound_Lead";
    public const string Error_NotFound_User = "Error_NotFound_User";
    public const string Error_NotFound_Policy = "Error_NotFound_Policy";
    public const string Error_NotFound_Workflow = "Error_NotFound_Workflow";
    public const string Error_NotFound_Tenant = "Error_NotFound_Tenant";
    public const string Error_TenantMismatch = "Error_TenantMismatch";
}
