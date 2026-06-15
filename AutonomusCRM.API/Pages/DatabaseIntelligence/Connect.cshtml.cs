using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using AutonomusCRM.Application.Authorization.Policies;
using AutonomusCRM.Application.DatabaseIntelligence;
using AutonomusCRM.API.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AutonomusCRM.API.Pages.DatabaseIntelligence;

[Authorize(Roles = "Admin,Owner")]
public class ConnectModel : PageModel
{
    private readonly IDbConnectionProfileService _connections;
    private readonly IServiceProvider _sp;

    public ConnectModel(IDbConnectionProfileService connections, IServiceProvider sp)
    {
        _connections = connections;
        _sp = sp;
    }

    [BindProperty]
    public ConnectInput Input { get; set; } = new();

    public int Step { get; private set; } = 1;
    public DbConnectionTestResultDto? TestResult { get; private set; }
    public string? StatusMessage { get; private set; }
    public bool IsSuccess { get; private set; }
    public bool CanSave => User.IsInRole("Admin") || User.IsInRole("Owner");

    public IReadOnlyList<DbEngineType> EngineOptions { get; } =
    [
        DbEngineType.PostgreSQL,
        DbEngineType.SqlServer,
        DbEngineType.MySQL,
        DbEngineType.MariaDB,
        DbEngineType.Oracle
    ];

    public void OnGet(int step = 1)
    {
        Step = Math.Clamp(step, 1, 3);
        Input.Port = Input.Port == 0 ? 5432 : Input.Port;
        if (Input.EngineType == default)
            Input.EngineType = DbEngineType.PostgreSQL;
    }

    public IActionResult OnPostSelectEngine(DbEngineType engineType)
    {
        Input.EngineType = engineType;
        Input.Port = DefaultPort(engineType);
        Step = 2;
        return Page();
    }

    public async Task<IActionResult> OnPostEnterDetails(CancellationToken cancellationToken)
    {
        Step = 3;
        try
        {
            TestResult = await _connections.TestAsync(new TestDbConnectionRequest(
                Input.EngineType,
                Input.Host,
                Input.Port,
                Input.DatabaseName,
                Input.Username,
                Input.Password,
                Input.IsReadOnly), cancellationToken);
            IsSuccess = TestResult.Success;
            StatusMessage = TestResult.Success
                ? "Great — we can reach your database."
                : TestResult.Message;
        }
        catch (DbIntelligenceValidationException ex)
        {
            Step = 2;
            StatusMessage = ex.Message;
        }

        return Page();
    }

    public async Task<IActionResult> OnPostSave(CancellationToken cancellationToken)
    {
        Step = 3;
        try
        {
            var tenantId = await this.GetTenantIdForPageAsync(_sp, cancellationToken);
            var userId = GetUserId();
            await _connections.CreateAsync(
                tenantId,
                userId,
                new CreateDbConnectionProfileRequest(
                    Input.Name,
                    Input.EngineType,
                    Input.Host,
                    Input.Port,
                    Input.DatabaseName,
                    Input.Username,
                    Input.Password,
                    Input.IsReadOnly),
                HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.Headers.UserAgent.ToString(),
                cancellationToken);

            return RedirectToPage("/DatabaseIntelligence/Index");
        }
        catch (DbIntelligenceValidationException ex)
        {
            StatusMessage = ex.Message;
            TestResult = await _connections.TestAsync(new TestDbConnectionRequest(
                Input.EngineType, Input.Host, Input.Port, Input.DatabaseName,
                Input.Username, Input.Password, Input.IsReadOnly), cancellationToken);
            return Page();
        }
        catch (DbIntelligenceQuotaException ex)
        {
            StatusMessage = ex.Message;
            return Page();
        }
    }

    private static int DefaultPort(DbEngineType engine) => engine switch
    {
        DbEngineType.PostgreSQL => 5432,
        DbEngineType.SqlServer => 1433,
        DbEngineType.MySQL => 3306,
        DbEngineType.MariaDB => 3306,
        DbEngineType.Oracle => 1521,
        _ => 5432
    };

    private Guid GetUserId()
    {
        var claim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst("sub")?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    public sealed class ConnectInput
    {
        public DbEngineType EngineType { get; set; } = DbEngineType.PostgreSQL;

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string Host { get; set; } = "localhost";
        public int Port { get; set; } = 5432;
        public string DatabaseName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public bool IsReadOnly { get; set; } = true;
    }
}
