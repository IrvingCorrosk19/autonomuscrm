namespace AutonomusCRM.Infrastructure.CustomerSuccess;

internal static class CommunicationTemplates
{
    private static readonly IReadOnlyDictionary<string, (string Subject, string Body)> EmailEn =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = ("Welcome to AutonomusFlow", "Hello {{name}}, thank you for trusting us. Your CS team will contact you soon."),
            ["onboarding"] = ("Your onboarding has started", "Hello {{name}}, we have started your onboarding plan. Review your assigned tasks."),
            ["followup"] = ("Account follow-up", "Hello {{name}}, we would like to know how your experience with the platform is going."),
            ["renewal"] = ("Upcoming renewal", "Hello {{name}}, your contract renews on {{renewal_date}}. Let's coordinate the renewal."),
            ["risk"] = ("Important: your account needs attention", "Hello {{name}}, we detected risk signals. Our CS team will contact you."),
            ["reengagement"] = ("We miss you", "Hello {{name}}, it has been a while. Can we help you get back on track?")
        };

    private static readonly IReadOnlyDictionary<string, (string Subject, string Body)> EmailEs =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = ("Bienvenido a AutonomusFlow", "Hola {{name}}, gracias por confiar en nosotros. Tu equipo CS te contactará pronto."),
            ["onboarding"] = ("Tu onboarding ha comenzado", "Hola {{name}}, hemos iniciado tu plan de onboarding. Revisa las tareas asignadas."),
            ["followup"] = ("Seguimiento de tu cuenta", "Hola {{name}}, queremos saber cómo va tu experiencia con la plataforma."),
            ["renewal"] = ("Próxima renovación", "Hola {{name}}, tu contrato renueva el {{renewal_date}}. Coordinemos la renovación."),
            ["risk"] = ("Importante: tu cuenta necesita atención", "Hola {{name}}, detectamos señales de riesgo. Nuestro equipo CS te contactará."),
            ["reengagement"] = ("Te extrañamos", "Hola {{name}}, hace tiempo sin actividad. ¿Podemos ayudarte a retomar el uso?")
        };

    private static readonly IReadOnlyDictionary<string, (string Subject, string Body)> EmailEsPa =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = ("Bienvenido a AutonomusFlow", "Estimado(a) {{name}}, gracias por confiar en nosotros. Su equipo de CS lo contactará pronto."),
            ["onboarding"] = ("Su onboarding ha comenzado", "Estimado(a) {{name}}, hemos iniciado su plan de onboarding. Revise las tareas asignadas."),
            ["followup"] = ("Seguimiento de su cuenta", "Estimado(a) {{name}}, queremos conocer cómo va su experiencia con la plataforma."),
            ["renewal"] = ("Próxima renovación", "Estimado(a) {{name}}, su contrato renueva el {{renewal_date}}. Coordinemos la renovación."),
            ["risk"] = ("Importante: su cuenta necesita atención", "Estimado(a) {{name}}, detectamos señales de riesgo. Nuestro equipo CS lo contactará."),
            ["reengagement"] = ("Le extrañamos", "Estimado(a) {{name}}, hace tiempo sin actividad. ¿Podemos ayudarle a retomar el uso?")
        };

    private static readonly IReadOnlyDictionary<string, string> WhatsAppEn =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = "Hello {{name}}, welcome to AutonomusFlow. Your CS Manager is available.",
            ["reminder"] = "Reminder: {{message}}",
            ["renewal"] = "Hello {{name}}, your renewal is on {{renewal_date}}. Shall we schedule a call?",
            ["followup"] = "AutonomusFlow follow-up: how is everything, {{name}}?",
            ["recovery"] = "Hello {{name}}, we noticed inactivity on your account. How can we help?"
        };

    private static readonly IReadOnlyDictionary<string, string> WhatsAppEs =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = "Hola {{name}}, bienvenido a AutonomusFlow. Tu CS Manager está disponible.",
            ["reminder"] = "Recordatorio: {{message}}",
            ["renewal"] = "Hola {{name}}, tu renovación es el {{renewal_date}}. ¿Agendamos una llamada?",
            ["followup"] = "Seguimiento AutonomusFlow: ¿cómo va todo, {{name}}?",
            ["recovery"] = "Hola {{name}}, vimos inactividad en tu cuenta. ¿En qué podemos ayudarte?"
        };

    private static readonly IReadOnlyDictionary<string, string> WhatsAppEsPa =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = "Hola {{name}}, bienvenido a AutonomusFlow. Su CS Manager está disponible.",
            ["reminder"] = "Recordatorio: {{message}}",
            ["renewal"] = "Hola {{name}}, su renovación es el {{renewal_date}}. ¿Agendamos una llamada?",
            ["followup"] = "Seguimiento AutonomusFlow: ¿cómo va todo, {{name}}?",
            ["recovery"] = "Hola {{name}}, notamos inactividad en su cuenta. ¿En qué podemos ayudarle?"
        };

    public static (string Subject, string Body) GetEmailTemplate(string templateKey, string? culture = null)
    {
        var dict = ResolveEmailDictionary(culture);
        if (dict.TryGetValue(templateKey, out var tpl))
            return tpl;
        return IsSpanish(culture)
            ? ("Notificación AutonomusFlow", "Mensaje del sistema.")
            : ("AutonomusFlow notification", "System message.");
    }

    public static string GetWhatsAppTemplate(string templateKey, string? culture = null)
    {
        var dict = ResolveWhatsAppDictionary(culture);
        return dict.TryGetValue(templateKey, out var tpl)
            ? tpl
            : IsSpanish(culture) ? "Mensaje AutonomusFlow." : "AutonomusFlow message.";
    }

    public static string Render(string template, IReadOnlyDictionary<string, string>? variables)
    {
        if (variables == null)
            return template;
        var result = template;
        foreach (var kv in variables)
            result = result.Replace("{{" + kv.Key + "}}", kv.Value, StringComparison.OrdinalIgnoreCase);
        return result;
    }

    private static IReadOnlyDictionary<string, (string Subject, string Body)> ResolveEmailDictionary(string? culture)
    {
        if (IsPanamaSpanish(culture)) return EmailEsPa;
        if (IsSpanish(culture)) return EmailEs;
        return EmailEn;
    }

    private static IReadOnlyDictionary<string, string> ResolveWhatsAppDictionary(string? culture)
    {
        if (IsPanamaSpanish(culture)) return WhatsAppEsPa;
        if (IsSpanish(culture)) return WhatsAppEs;
        return WhatsAppEn;
    }

    private static bool IsPanamaSpanish(string? culture) =>
        !string.IsNullOrWhiteSpace(culture)
        && culture.StartsWith("es-PA", StringComparison.OrdinalIgnoreCase);

    private static bool IsSpanish(string? culture)
    {
        if (string.IsNullOrWhiteSpace(culture))
            return string.Equals(System.Globalization.CultureInfo.CurrentUICulture.TwoLetterISOLanguageName, "es", StringComparison.OrdinalIgnoreCase);
        return culture.StartsWith("es", StringComparison.OrdinalIgnoreCase);
    }
}
