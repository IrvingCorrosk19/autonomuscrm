using AutonomusCRM.API.Resources;
using AutonomusCRM.Domain.Deals;
using Microsoft.Extensions.Localization;

namespace AutonomusCRM.API.Infrastructure;

public static class LocalizedLabels
{
    public static string DealStageLabel(IStringLocalizer<SharedResource> l, DealStage stage) => stage switch
    {
        DealStage.Prospecting => l["DealStage_Prospecting"].Value,
        DealStage.Qualification => l["DealStage_Qualification"].Value,
        DealStage.Proposal => l["DealStage_Proposal"].Value,
        DealStage.Negotiation => l["DealStage_Negotiation"].Value,
        DealStage.ClosedWon => l["DealStage_ClosedWon"].Value,
        DealStage.ClosedLost => l["DealStage_ClosedLost"].Value,
        _ => stage.ToString()
    };

    public static string DealStageFromName(IStringLocalizer<SharedResource> l, string name) =>
        Enum.TryParse<DealStage>(name, out var stage) ? DealStageLabel(l, stage) : name;

    public static string DealStatusLabel(IStringLocalizer<SharedResource> l, DealStatus status) => status switch
    {
        DealStatus.Open => l["DealStatus_Open"].Value,
        DealStatus.Closed => l["DealStatus_Closed"].Value,
        _ => status.ToString()
    };

    public static string Priority(IStringLocalizer<SharedResource> l, string priority) => priority switch
    {
        "Urgent" => l["Priority_Urgent"].Value,
        "High" => l["Priority_High"].Value,
        "Normal" => l["Priority_Normal"].Value,
        _ => priority
    };
}
