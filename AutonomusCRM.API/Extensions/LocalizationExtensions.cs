using System.Globalization;
using AutonomusCRM.API.Resources;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Options;

namespace AutonomusCRM.API.Extensions;

public static class LocalizationExtensions
{
    public static readonly string[] SupportedCultures = ["es", "es-PA", "en"];
    public const string DefaultCulture = "es";

    public static IServiceCollection AddAppLocalization(this IServiceCollection services)
    {
        // Embedded .resx use the type full name (AutonomusCRM.API.Resources.SharedResource).
        // Do not set ResourcesPath here — it would prepend "Resources." and break lookup.
        services.AddLocalization();

        services.AddSingleton<FlowClientStringsProvider>();

        services.Configure<RequestLocalizationOptions>(options =>
        {
            var supported = SupportedCultures
                .Select(c => new CultureInfo(c))
                .ToList();

            options.DefaultRequestCulture = new RequestCulture(DefaultCulture);
            options.SupportedCultures = supported;
            options.SupportedUICultures = supported;

            options.RequestCultureProviders.Clear();
            options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
            options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
        });

        return services;
    }

    public static IMvcBuilder AddAppViewLocalization(this IMvcBuilder mvc)
    {
        mvc.AddViewLocalization(LanguageViewLocationExpanderFormat.Suffix)
            .AddDataAnnotationsLocalization(options =>
            {
                options.DataAnnotationLocalizerProvider = (_, factory) =>
                    factory.Create(typeof(ValidationMessages));
            });

        return mvc;
    }

    public static WebApplication UseAppLocalization(this WebApplication app)
    {
        var options = app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value;
        app.UseRequestLocalization(options);
        return app;
    }
}

/// <summary>Injects localized strings for client-side scripts (palette, toasts, routes).</summary>
public sealed class FlowClientStringsProvider(IStringLocalizer<SharedResource> localizer)
{
    public IReadOnlyDictionary<string, string> GetStrings()
    {
        string L(string key) => localizer[key].Value;

        return new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["noResults"] = L("Palette_NoResults"),
            ["typeRoute"] = L("Palette_TypeRoute"),
            ["typeLead"] = L("Palette_TypeLead"),
            ["typeCustomer"] = L("Palette_TypeCustomer"),
            ["typeDeal"] = L("Palette_TypeDeal"),
            ["typeCommand"] = L("Palette_TypeCommand"),
            ["toastSuccess"] = L("Toast_Success"),
            ["toastError"] = L("Toast_Error"),
            ["toastWarning"] = L("Toast_Warning"),
            ["toastInfo"] = L("Toast_Info"),
            ["toastReady"] = L("Toast_Ready"),
            ["toastOnboardingReset"] = L("Toast_OnboardingReset"),
            ["operationInProgress"] = L("Operation_InProgress"),
            ["operationProcessing"] = L("Operation_Processing"),
            ["operationCompleted"] = L("Operation_Completed"),
            ["operationCompletedTitle"] = L("Operation_CompletedTitle"),
            ["operationFailed"] = L("Operation_Failed"),
            ["operationErrorTitle"] = L("Operation_ErrorTitle"),
            ["operationRetry"] = L("Operation_Retry"),
            ["darkMode"] = L("Shell_DarkMode"),
            ["lightMode"] = L("Shell_LightMode"),
            ["searchPlaceholder"] = L("Shell_PalettePlaceholder"),
            ["moduleDashboard"] = L("Module_Dashboard"),
            ["moduleOperation"] = L("Module_Operation"),
            ["modulePrevious"] = L("Module_PreviousModule"),
            ["moduleOperationDefault"] = L("Module_OperationDefault")
        };
    }

    public IReadOnlyList<FlowClientRoute> GetRoutes()
    {
        string L(string key) => localizer[key].Value;

        return
        [
            new(L("Route_FlowCommand"), "/", L("Route_Group_Command")),
            new(L("Route_TrustStudio"), "/TrustInbox", L("Route_Group_Command")),
            new(L("Route_Workforce"), "/Agents", L("Route_Group_Command")),
            new(L("Route_DecisionsHistory"), "/command/decisions", L("Route_Group_Command")),
            new(L("Route_Outcomes"), "/command/outcomes", L("Route_Group_Command")),
            new(L("Route_Playbooks"), "/command/playbooks", L("Route_Group_Command")),
            new(L("Route_RevenueOs"), "/revenue", L("Route_Group_Revenue")),
            new(L("Route_ExecutiveIntel"), "/executive", L("Route_Group_Revenue")),
            new(L("Route_Pipeline"), "/Deals", L("Route_Group_Revenue")),
            new(L("Route_Leads"), "/Leads", L("Route_Group_Commerce")),
            new(L("Route_Customers"), "/Customers", L("Route_Group_Customers")),
            new(L("Route_Customer360"), "/Customer360", L("Route_Group_Customers")),
            new(L("Route_CustomerSuccess"), "/customer-success", L("Route_Group_Customers")),
            new(L("Route_Memory"), "/Memory", L("Route_Group_Intelligence")),
            new(L("Route_Billing"), "/billing", L("Route_Group_Platform")),
            new(L("Route_Integrations"), "/Integrations", L("Route_Group_Platform")),
            new(L("Route_Voice"), "/VoiceCalls", L("Route_Group_Platform")),
            new(L("Route_Settings"), "/Settings", L("Route_Group_Admin")),
            new(L("Route_Users"), "/Users", L("Route_Group_Admin")),
            new(L("Route_Audit"), "/Audit", L("Route_Group_Admin")),
            new(L("Route_Policies"), "/Policies", L("Route_Group_Admin")),
            new(L("Route_Tasks"), "/Tasks", L("Route_Group_Operation")),
            new(L("Route_Workflows"), "/Workflows", L("Route_Group_Operation"))
        ];
    }
}

public sealed record FlowClientRoute(string Name, string Path, string Group);
