using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DataHub;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class RulesModel : PageModel
{
    private readonly IDataHubRulesEngineService _rules;
    private readonly IDataHubFieldCatalog _fields;
    private readonly IServiceProvider _sp;

    public RulesModel(IDataHubRulesEngineService rules, IDataHubFieldCatalog fields, IServiceProvider sp)
    {
        _rules = rules;
        _fields = fields;
        _sp = sp;
    }

    public string[] Entities { get; } = ["Customer", "Lead", "Deal", "User"];
    public string TargetEntity { get; set; } = "Customer";
    public IReadOnlyList<DataHubVisualRuleDto> Rules { get; set; } = Array.Empty<DataHubVisualRuleDto>();
    public IReadOnlyList<DataHubFieldDefinition> Fields { get; set; } = Array.Empty<DataHubFieldDefinition>();
    public DataHubRuleSetVersionDto? RuleSetVersion { get; set; }
    public string? Message { get; set; }

    public string[] Operators { get; } = ["equals", "empty", "notempty"];
    public string[] Actions { get; } = ["SetValue", "Transform", "MarkError", "MarkReview"];

    public async Task OnGetAsync(string? targetEntity, CancellationToken cancellationToken)
    {
        TargetEntity = targetEntity ?? "Customer";
        await LoadAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostSaveAsync(string targetEntity, List<DataHubVisualRuleDto>? rules, CancellationToken cancellationToken)
    {
        TargetEntity = targetEntity;
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        if (rules == null || rules.Count == 0)
        {
            rules = _rules.GetDefaultRules(TargetEntity).ToList();
        }

        for (var i = 0; i < rules.Count; i++)
        {
            rules[i] = rules[i] with { Priority = i + 1 };
        }

        RuleSetVersion = await _rules.SaveRulesAsync(tenantId, TargetEntity, rules, cancellationToken);
        Message = $"Rules saved — version {RuleSetVersion.Version} ({RuleSetVersion.RuleCount} rules).";
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostResetAsync(string targetEntity, CancellationToken cancellationToken)
    {
        TargetEntity = targetEntity;
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        var defaults = _rules.GetDefaultRules(TargetEntity).ToList();
        RuleSetVersion = await _rules.SaveRulesAsync(tenantId, TargetEntity, defaults, cancellationToken);
        Message = "Default rules restored.";
        await LoadAsync(cancellationToken);
        return Page();
    }

    private async Task LoadAsync(CancellationToken cancellationToken)
    {
        var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
        Fields = _fields.GetFields(TargetEntity);
        Rules = await _rules.GetRulesAsync(tenantId, TargetEntity, cancellationToken);
        RuleSetVersion = await _rules.GetRuleSetVersionAsync(tenantId, TargetEntity, cancellationToken);
    }
}
