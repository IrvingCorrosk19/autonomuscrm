using AutonomusCRM.Application.Autonomous;
using AutonomusCRM.Application.DataPlatform;

namespace AutonomusCRM.API.Infrastructure;

public static class FlowInsightTypes
{
    public const string Risk = "risk";
    public const string Expansion = "expansion";
    public const string Renewal = "renewal";
    public const string RevenueAtRisk = "revenue_at_risk";

    public static string DefaultRecommendation(string insightType, string? taskType = null) => insightType switch
    {
        Risk => taskType switch
        {
            "Call" => "Programar llamada de retención",
            "Retention" => "Crear tarea de retención",
            "Email" => "Enviar email de seguimiento",
            "TrustApproval" => "Solicitar aprobación humana",
            _ => "Intervenir cliente en riesgo"
        },
        Expansion => taskType switch
        {
            "Proposal" => "Generar propuesta de expansión",
            "Meeting" => "Programar reunión de expansión",
            "CreateDeal" => "Crear oportunidad de expansión",
            _ => "Enviar propuesta de expansión"
        },
        Renewal => taskType switch
        {
            "Renewal" => "Preparar renovación",
            "Email" => "Contactar cliente por renovación",
            _ => "Gestionar renovación próxima"
        },
        RevenueAtRisk => "Crear plan de recuperación de revenue",
        _ => "Acción recomendada por ABOS"
    };
}

public static class FlowInsightMapper
{
    public static string FromInsightCategory(string? category) => category?.ToLowerInvariant() switch
    {
        "risk" => FlowInsightTypes.Risk,
        "expansion" => FlowInsightTypes.Expansion,
        "opportunity" => FlowInsightTypes.Expansion,
        "renewal" => FlowInsightTypes.Renewal,
        _ => FlowInsightTypes.Risk
    };

    public static string FromNba(NextBestActionDto nba)
    {
        var action = nba.RecommendedAction;
        if (action.Contains("Renewal", StringComparison.OrdinalIgnoreCase)
            || action.Contains("Renew", StringComparison.OrdinalIgnoreCase))
            return FlowInsightTypes.Renewal;
        if (action.Contains("Expansion", StringComparison.OrdinalIgnoreCase)
            || action.Contains("Upsell", StringComparison.OrdinalIgnoreCase))
            return FlowInsightTypes.Expansion;
        return FlowInsightTypes.Risk;
    }

    public static string FromHealth(CustomerHealthCenterDto health)
    {
        if (health.ExpansionReadiness >= 60 && (health.ChurnRisk ?? 0) < 40)
            return FlowInsightTypes.Expansion;
        if (health.ChurnRisk >= 50 || health.RiskLevel is "Alto" or "High")
            return FlowInsightTypes.Risk;
        return FlowInsightTypes.Risk;
    }

    public static string FromCustomer360(Customer360Dto customer)
    {
        if (customer.ChurnRisk >= 50)
            return FlowInsightTypes.Risk;
        if (customer.WonRevenue >= 10_000m && (customer.ChurnRisk ?? 0) < 30)
            return FlowInsightTypes.Expansion;
        return FlowInsightTypes.Risk;
    }
}
