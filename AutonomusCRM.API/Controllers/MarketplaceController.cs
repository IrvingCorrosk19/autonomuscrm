using AutonomusCRM.Application.DataPlatform;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/marketplace")]
public class MarketplaceController : ControllerBase
{
    private readonly IMarketplaceCatalogService _catalog;

    public MarketplaceController(IMarketplaceCatalogService catalog) => _catalog = catalog;

    [HttpGet("extensions")]
    [AllowAnonymous]
    public IActionResult Extensions() => Ok(_catalog.ListExtensions());

    [HttpGet("sdk")]
    [AllowAnonymous]
    public IActionResult SdkManifest() => Ok(new
    {
        version = "1.0.0",
        openapi = "/swagger/v1/swagger.json",
        scopes = new[] { "crm.read", "crm.write", "ai.audit", "billing.read" },
        webhooks = new[] { "/api/webhooks/usage/{tenantId}", "/api/webhooks/crm/customers/{tenantId}" }
    });
}
