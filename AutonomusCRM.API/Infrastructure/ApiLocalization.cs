using AutonomusCRM.API.Resources;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Infrastructure;

public static class ApiLocalization
{
    public static object Error(IStringLocalizer<SharedResource> localizer, string keyOrMessage, params object[] args)
    {
        var localized = args.Length > 0 ? localizer[keyOrMessage, args] : localizer[keyOrMessage];
        var text = localized.ResourceNotFound ? keyOrMessage : localized.Value;
        return new { error = text };
    }

    public static string Text(IStringLocalizer<SharedResource> localizer, string keyOrMessage, params object[] args)
    {
        var localized = args.Length > 0 ? localizer[keyOrMessage, args] : localizer[keyOrMessage];
        return localized.ResourceNotFound ? keyOrMessage : localized.Value;
    }

    public static string Message(IStringLocalizer<SharedResource> localizer, string keyOrMessage, params object[] args)
        => Text(localizer, keyOrMessage, args);
}
