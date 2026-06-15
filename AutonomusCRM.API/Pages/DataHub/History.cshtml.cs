using AutonomusCRM.Application.Authorization.Policies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DataHub;

[Authorize(Policy = AuthorizationPolicies.RequireManager)]
public class HistoryModel : JobsModel
{
    public HistoryModel(Application.DataHub.IDataHubRepository repo, IServiceProvider sp) : base(repo, sp) { }
}
