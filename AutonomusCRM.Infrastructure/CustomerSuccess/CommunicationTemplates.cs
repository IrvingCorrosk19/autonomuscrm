namespace AutonomusCRM.Infrastructure.CustomerSuccess;

internal static class CommunicationTemplates
{
    public static readonly IReadOnlyDictionary<string, (string Subject, string Body)> EmailTemplates =
        new Dictionary<string, (string, string)>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = ("Bienvenido a AutonomusFlow", "Hola {{name}}, gracias por confiar en nosotros. Tu equipo CS te contactará pronto."),
            ["onboarding"] = ("Tu onboarding ha comenzado", "Hola {{name}}, hemos iniciado tu plan de onboarding. Revisa las tareas asignadas."),
            ["followup"] = ("Seguimiento de tu cuenta", "Hola {{name}}, queremos saber cómo va tu experiencia con la plataforma."),
            ["renewal"] = ("Próxima renovación", "Hola {{name}}, tu contrato renueva el {{renewal_date}}. Coordinemos la renovación."),
            ["risk"] = ("Importante: tu cuenta necesita atención", "Hola {{name}}, detectamos señales de riesgo. Nuestro equipo CS te contactará."),
            ["reengagement"] = ("Te extrañamos", "Hola {{name}}, hace tiempo sin actividad. ¿Podemos ayudarte a retomar el uso?")
        };

    public static readonly IReadOnlyDictionary<string, string> WhatsAppTemplates =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["welcome"] = "Hola {{name}}, bienvenido a AutonomusFlow. Tu CS Manager está disponible.",
            ["reminder"] = "Recordatorio: {{message}}",
            ["renewal"] = "Hola {{name}}, tu renovación es el {{renewal_date}}. ¿Agendamos una llamada?",
            ["followup"] = "Seguimiento AutonomusFlow: ¿cómo va todo, {{name}}?",
            ["recovery"] = "Hola {{name}}, vimos inactividad en tu cuenta. ¿En qué podemos ayudarte?"
        };

    public static string Render(string template, IReadOnlyDictionary<string, string>? variables)
    {
        if (variables == null)
            return template;
        var result = template;
        foreach (var kv in variables)
            result = result.Replace("{{" + kv.Key + "}}", kv.Value, StringComparison.OrdinalIgnoreCase);
        return result;
    }
}
