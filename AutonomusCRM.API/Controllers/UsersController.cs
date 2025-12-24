using AutonomusCRM.Application.Users.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IRequestHandler<CreateUserCommand, Guid> _createHandler;
    private readonly IRequestHandler<EnableMfaCommand, EnableMfaResult> _enableMfaHandler;
    private readonly ILogger<UsersController> _logger;

    public UsersController(
        IRequestHandler<CreateUserCommand, Guid> createHandler,
        IRequestHandler<EnableMfaCommand, EnableMfaResult> enableMfaHandler,
        ILogger<UsersController> logger)
    {
        _createHandler = createHandler;
        _enableMfaHandler = enableMfaHandler;
        _logger = logger;
    }

    [HttpPost]
    [Authorize(Policy = "RequireAdmin")]
    public async Task<ActionResult<Guid>> CreateUser([FromBody] CreateUserCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var userId = await _createHandler.HandleAsync(command, cancellationToken);
            return CreatedAtAction(nameof(GetUser), new { id = userId }, userId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating user");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public ActionResult GetUser(Guid id)
    {
        // TODO: Implementar GetUserQuery
        return Ok(new { id });
    }

    [HttpPost("{id}/enable-mfa")]
    public async Task<ActionResult<EnableMfaResult>> EnableMfa(Guid id, [FromQuery] Guid tenantId, CancellationToken cancellationToken)
    {
        try
        {
            var command = new EnableMfaCommand(id, tenantId);
            var result = await _enableMfaHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error enabling MFA");
            return BadRequest(new { error = ex.Message });
        }
    }
}

