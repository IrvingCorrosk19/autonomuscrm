using AutonomusCRM.Application.Auth.Commands;
using AutonomusCRM.Application.Common.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace AutonomusCRM.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IRequestHandler<LoginCommand, LoginResult> _loginHandler;
    private readonly IRequestHandler<VerifyMfaCommand, LoginResult> _verifyMfaHandler;
    private readonly ILogger<AuthController> _logger;

    public AuthController(
        IRequestHandler<LoginCommand, LoginResult> loginHandler,
        IRequestHandler<VerifyMfaCommand, LoginResult> verifyMfaHandler,
        ILogger<AuthController> logger)
    {
        _loginHandler = loginHandler;
        _verifyMfaHandler = verifyMfaHandler;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<ActionResult<LoginResult>> Login([FromBody] LoginCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _loginHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Login failed for {Email}", command.Email);
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during login");
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("verify-mfa")]
    public async Task<ActionResult<LoginResult>> VerifyMfa([FromBody] VerifyMfaCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _verifyMfaHandler.HandleAsync(command, cancellationToken);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "MFA verification failed");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during MFA verification");
            return BadRequest(new { error = ex.Message });
        }
    }
}

