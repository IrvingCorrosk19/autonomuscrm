using AutonomusCRM.Application.Common.Interfaces;
using AutonomusCRM.Application.Common.Tenancy;
using AutonomusCRM.Application.Deals.Queries;
using AutonomusCRM.Domain.Deals;
using AutonomusCRM.Domain.Leads;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/flow")]
[Authorize]
public class FlowSearchController : ControllerBase
{
    private readonly ITenantContext _tenant;
    private readonly ILeadRepository _leads;
    private readonly ICustomerRepository _customers;
    private readonly IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>> _deals;

    public FlowSearchController(
        ITenantContext tenant,
        ILeadRepository leads,
        ICustomerRepository customers,
        IRequestHandler<GetDealsByTenantQuery, IEnumerable<DealDto>> deals)
    {
        _tenant = tenant;
        _leads = leads;
        _customers = customers;
        _deals = deals;
    }

    [HttpGet("search")]
    public async Task<ActionResult<FlowSearchResponse>> Search([FromQuery] string? q, CancellationToken cancellationToken)
    {
        if (_tenant.TenantId is not Guid tenantId)
            return Unauthorized();

        var term = (q ?? "").Trim();
        if (term.Length < 2)
            return Ok(new FlowSearchResponse(Array.Empty<FlowSearchItem>(), Array.Empty<FlowSearchItem>(),
                Array.Empty<FlowSearchItem>(), Array.Empty<FlowSearchItem>(), Array.Empty<FlowSearchItem>()));

        var t = term.ToLowerInvariant();

        var leadItems = (await _leads.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(l => Matches(l.Name, l.Email, l.Company, t))
            .Take(8)
            .Select(l => new FlowSearchItem("lead", l.Name, $"/Leads/Details/{l.Id}", l.Company ?? l.Email ?? ""))
            .ToList();

        var customerItems = (await _customers.GetByTenantIdAsync(tenantId, cancellationToken))
            .Where(c => Matches(c.Name, c.Email, c.Company, t))
            .Take(8)
            .Select(c => new FlowSearchItem("customer", c.Name, $"/customers/{c.Id}/360", c.Company ?? ""))
            .ToList();

        var deals = await _deals.HandleAsync(new GetDealsByTenantQuery(tenantId), cancellationToken);
        var dealItems = deals
            .Where(d => d.Title != null && d.Title.ToLowerInvariant().Contains(t))
            .Take(8)
            .Select(d => new FlowSearchItem("deal", d.Title!, $"/Deals/Details/{d.Id}", $"${d.Amount:N0} · {d.Stage}"))
            .ToList();

        var routes = new[]
        {
            ("trust", "Trust Studio", "/TrustInbox", ""),
            ("outcome", "Outcomes", "/command/outcomes", ""),
            ("playbook", "Playbooks", "/command/playbooks", ""),
            ("revenue", "Revenue OS", "/revenue", ""),
            ("integration", "Integraciones", "/Integrations", "")
        };
        var routeItems = routes
            .Where(r => r.Item2.ToLowerInvariant().Contains(t))
            .Select(r => new FlowSearchItem(r.Item1, r.Item2, r.Item3, r.Item4))
            .ToList();

        return Ok(new FlowSearchResponse(leadItems, customerItems, dealItems, routeItems, Array.Empty<FlowSearchItem>()));
    }

    private static bool Matches(string? a, string? b, string? c, string term) =>
        (a?.ToLowerInvariant().Contains(term) ?? false) ||
        (b?.ToLowerInvariant().Contains(term) ?? false) ||
        (c?.ToLowerInvariant().Contains(term) ?? false);
}

public record FlowSearchItem(string Type, string Title, string Href, string Subtitle);

public record FlowSearchResponse(
    IReadOnlyList<FlowSearchItem> Leads,
    IReadOnlyList<FlowSearchItem> Customers,
    IReadOnlyList<FlowSearchItem> Deals,
    IReadOnlyList<FlowSearchItem> Routes,
    IReadOnlyList<FlowSearchItem> Integrations);
